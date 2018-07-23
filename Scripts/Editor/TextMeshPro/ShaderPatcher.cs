using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SoftMasking.TextMeshPro.Editor {
    public class PatchException : Exception {
        public PatchException(string message) : base(message) { }
    }

    public static class ShaderPatcher {
        public static string Patch(string shader) {
            var names = Analyze(shader);
            var result = shader;
            result = UpdateShaderName(result);
            result = InjectProperty(result);
            result = InjectPragma(result);
            result = InjectInclude(result);
            result = InjectV2FFields(result, names.v2fStruct);
            result = InjectVertexInstructions(result, names.v2fStruct, names.v2fPositionField, names.vertShader);
            result = InjectFragmentInstructions(result, names.fragShader);
            result = FixVertexInitialization(result, names.v2fStruct);
            return result;
        }

        struct Names {
            public string vertShader;
            public string fragShader;
            public string v2fStruct;
            public string v2fPositionField;
        }

        static readonly Regex VERTEX_PRAMGA_PATTERN = new Regex(@"#pragma\s+vertex\s+(\w+)");
        static readonly Regex FRAGMENT_PRAMGA_PATTERN = new Regex(@"#pragma\s+fragment\s+(\w+)");

        static Names Analyze(string shader) {
            var vertShader =
                EnsureMatch(VERTEX_PRAMGA_PATTERN, shader, "Unable to find vertex shader #pragma")
                    .Groups[1].Value;
            var fragShader =
                EnsureMatch(FRAGMENT_PRAMGA_PATTERN, shader, "Unable to find fragment shader #pragma")
                    .Groups[1].Value;
            var vertReturnTypePattern = new Regex(@"(\w*)\s*" + vertShader + @"\s*\([^)]*\)");
            var match = EnsureMatch(vertReturnTypePattern, shader, "Unable to find V2F struct declaration");
            var v2fStruct = match.Groups[1].Value;
            var v2fPositionFieldPattern = new Regex(@"struct\s+" + v2fStruct + @"\s*\{[^}]*float\d\s*(\w+)\s*:\s*(?:SV_)?POSITION;[^}]*\}");
            var v2fPositionField = EnsureMatch(v2fPositionFieldPattern, shader, "Unable to determine V2F position field").Groups[1].Value;
            return new Names {
                vertShader = vertShader,
                fragShader = fragShader,
                v2fStruct = v2fStruct,
                v2fPositionField = v2fPositionField
            };
        }

        static readonly Regex SHADER_NAME_PATTERN = new Regex(@"Shader\s+""([^""]+)""");

        static string UpdateShaderName(string shader) {
            var match = EnsureMatch(SHADER_NAME_PATTERN, shader, "Unable to find shader declaration");
            return shader.Insert(match.Groups[1].Index, "Soft Mask/");
        }

        static readonly Regex PROPERTIES_PATTERN = new Regex(
            @"Properties\s*\{" +
                @"(?:[^{}]|(?<open>\{)|(?<-open>\}))*" + // Swallow all the content with balancing
                @"(?(open)(?!))" + // Match only if braces are balanced
                @"()$" + // Capture a point on the line end of last property
            @"(\s*)\}", // Capture whitespace before ending } to keep indent
            RegexOptions.Multiline);

        static string InjectProperty(string shader) {
            var match = EnsureMatch(PROPERTIES_PATTERN, shader, "Unable to inject Soft Mask property");
            var endOfLastProperty = match.Groups[1].Index;
            var padding = match.Groups[2].Value;
            return shader.Insert(endOfLastProperty,
                "\n" + padding + "\t_SoftMask(\"Mask\", 2D) = \"white\" {} // Soft Mask");
        }

        static readonly Regex LAST_PRAGMA_PATTERN = LastDirectivePattern("pragma");
        static readonly Regex LAST_INCLUDE_PATTERN = LastDirectivePattern("include");

        static Regex LastDirectivePattern(string directive) {
            return new Regex(
                @"^([ \t]*)#" + directive + @"[^\n]*()$",
                RegexOptions.Multiline);
        }

        static string InjectPragma(string shader) {
            var match = EnsureLastMatch(LAST_PRAGMA_PATTERN, shader, "Unable to inject Soft Mask #pragma");
            var padding = match.Groups[1].Value;
            return shader.Insert(match.Groups[2].Index,
                "\n" +
                "\n" + padding + "// Soft Mask" +
                "\n" + padding + "#pragma multi_compile __ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED\n");
        }

        static string InjectInclude(string shader) {
            var match = EnsureLastMatch(LAST_INCLUDE_PATTERN, shader, "Unable to inject Soft Mask #include");
            var padding = match.Groups[1].Value;
            return shader.Insert(match.Groups[2].Index,
                "\n" + padding + "#include \"SoftMask.cginc\" // Soft Mask");
        }

        static readonly Regex TEXCOORD_PATTERN = new Regex(@"TEXCOORD(\d+)");

        static string InjectV2FFields(string shader, string v2fStructName) {
            var pattern = new Regex(
                @"struct\s+" + v2fStructName + @"\s*\{[^}]*()$(\s*)\}",  // Do not balance braces -
                RegexOptions.Multiline);                                 // expecting no braces inside struct
            var match = EnsureMatch(pattern, shader, "Unable to inject Soft Mask V2F signature");
            var texcoords = TEXCOORD_PATTERN.Matches(match.Value);
            var maxUsedTexcoord =
                texcoords.OfType<Match>()
                    .Where(m => m.Success)
                    .Max(m => int.Parse(m.Groups[1].Value));
            var padding = match.Groups[2].Value;
            return shader.Insert(match.Groups[1].Index,
                "\n" + padding + "\tSOFTMASK_COORDS(" + (maxUsedTexcoord + 1) + ") // Soft Mask");
        }

        static Regex VertexFunctionPattern(string v2fStruct, string function) {
            return FunctionPattern(v2fStruct, function);
        }

        static Regex FunctionPattern(string returnType, string function) {
            // See PROPERTIES_PATTERN for some explanation of this regex
            return new Regex(
                returnType + @"\s+" + function + @"\s*\(\w+\s+(\w+)\)\s*(?::\s*[a-zA-Z0-9_]+\s*)?\{" +
                    @"(?:[^{}]|(?<open>\{)|(?<-open>\}))*" +
                    @"(?(open)(?!))" +
                    @"()$" +
                @"(\s*)\}", RegexOptions.Multiline);
        }

        static readonly Regex RETURN_PATTERN = new Regex(@"^([^\n]*)return\s+([^;]+);\s*$", RegexOptions.Multiline);

        static string InjectVertexInstructions(string shader, string v2fStruct, string v2fPositionField, string function) {
            return ModifyFunctionReturn(shader, v2fStruct, function, (varName, inputName) =>
                "SOFTMASK_CALCULATE_COORDS(" + varName + ", " + inputName + "." + v2fPositionField + ")"
            );
        }

        static string InjectFragmentInstructions(string shader, string function) {
            return ModifyFunctionReturn(shader, @"fixed4", function, (varName, inputName) =>
                varName + " *= SOFTMASK_GET_MASK(" + inputName + ");"
            );
        }

        static string ModifyFunctionReturn(
                string shader,
                string returnType,
                string function,
                Func<string, string, string> modify) {
            var functionMatch =
                EnsureMatch(
                    FunctionPattern(returnType, function),
                    shader,
                    "Unable to locate vertex shader function");
            var functionBody = functionMatch.Value;
            var inputName = functionMatch.Groups[1].Value;
            var returnMatch = EnsureMatch(RETURN_PATTERN, functionBody, "Unable to find vertex shader's return statement");
            var variableName = returnMatch.Groups[2].Value;
            var padding = returnMatch.Groups[1].Value;
            var sb = new StringBuilder(shader);
            var returnStart = functionMatch.Index + returnMatch.Index;
            sb.Remove(returnStart, returnMatch.Length);
            sb.Insert(returnStart,
                padding + "// Soft Mask\n" +
                padding + returnType + " result_SoftMask = " + variableName + ";\n" +
                padding + modify("result_SoftMask", inputName) + "\n" +
                padding + "return result_SoftMask;");
            return sb.ToString();
        }

        static string FixVertexInitialization(string shader, string v2fStruct) {
            var pattern = new Regex(v2fStruct + @"\s+\w+\s*=\s*\{[^}]*()$(\s*)\}", RegexOptions.Multiline);
            var match = pattern.Match(shader);
            if (match.Success) {
                var padding = match.Groups[2].Value;
                return shader.Insert(match.Groups[1].Index,
                    padding + "#ifdef __SOFTMASK_ENABLE" +
                    padding + "\tfloat4(0, 0, 0, 0), // Soft Mask" +
                    padding + "#endif");
            }
            return shader;
        }

        static Match EnsureMatch(Regex regex, string pattern, string errorMessage) {
            var match = regex.Match(pattern);
            if (!match.Success)
                throw new PatchException(errorMessage);
            return match;
        }

        static Match EnsureLastMatch(Regex regex, string pattern, string errorMessage) {
            Match match = null;
            Match lastMatch;
            do {
                lastMatch = match;
                match = regex.Match(pattern, lastMatch != null ? lastMatch.Index + lastMatch.Length : 0);
            } while (match.Success);
            if (lastMatch == null || !lastMatch.Success)
                throw new PatchException(errorMessage);
            return lastMatch;
        }
    }
}
