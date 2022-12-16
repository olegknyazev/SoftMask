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
            var names = DetectNames(shader);
            var blendingMode = DetectBlendingMode(shader);
            var result = shader;
            result = UpdateShaderName(result);
            result = InjectProperty(result);
            result = InjectPragma(result);
            result = InjectInclude(result);
            result = InjectV2FFields(result, names.v2fStruct);
            result = InjectVertexInstructions(result, names.v2fStruct, names.v2fPositionField, names.vertexShader);
            result = InjectFragmentInstructions(result, blendingMode, names.fragmentShader);
            result = FixVertexInitialization(result, names.v2fStruct);
            return result;
        }

        struct Names {
            public string vertexShader;
            public string fragmentShader;
            public string v2fStruct;
            public string v2fPositionField;
        }

        static readonly Regex VertexPragmaPattern = new Regex(@"#pragma\s+vertex\s+(\w+)");
        static readonly Regex FragmentPragmaPattern = new Regex(@"#pragma\s+fragment\s+(\w+)");

        static Names DetectNames(string shader) {
            var vertexShader =
                EnsureMatch(VertexPragmaPattern, shader, "Unable to find vertex shader #pragma")
                    .Groups[1].Value;
            var fragmentShader =
                EnsureMatch(FragmentPragmaPattern, shader, "Unable to find fragment shader #pragma")
                    .Groups[1].Value;
            var vertexShaderReturnTypePattern = new Regex(@"(\w*)\s*" + vertexShader + @"\s*\([^)]*\)");
            var match = EnsureMatch(vertexShaderReturnTypePattern, shader, "Unable to find V2F struct declaration");
            var v2fStruct = match.Groups[1].Value;
            var v2fPositionFieldPattern = new Regex(@"struct\s+" + v2fStruct + @"\s*\{[^}]*float\d\s*(\w+)\s*:\s*(?:SV_)?POSITION;[^}]*\}");
            var v2fPositionField = EnsureMatch(v2fPositionFieldPattern, shader, "Unable to determine V2F position field").Groups[1].Value;
            return new Names {
                vertexShader = vertexShader,
                fragmentShader = fragmentShader,
                v2fStruct = v2fStruct,
                v2fPositionField = v2fPositionField
            };
        }
        
        enum BlendingMode { PremultipliedAlpha, Classic }

        static readonly Regex BlendingPattern = new Regex(@"^\s*Blend\s+(\w+)\s+(\w+)", RegexOptions.Multiline);
        
        static BlendingMode DetectBlendingMode(string shader) {
            var match = EnsureMatch(BlendingPattern, shader, "Unable to determine shader blending mode");
            var sourceBlend = match.Groups[1].Value.ToLowerInvariant();
            var destinationBlend = match.Groups[2].Value.ToLowerInvariant();
            return sourceBlend == "one" && destinationBlend == "oneminussrcalpha"
                ? BlendingMode.PremultipliedAlpha
                : BlendingMode.Classic;
        }
        
        static readonly Regex ShaderNamePattern = new Regex(@"Shader\s+""([^""]+)""");

        static string UpdateShaderName(string shader) {
            var match = EnsureMatch(ShaderNamePattern, shader, "Unable to find shader declaration");
            return shader.Insert(match.Groups[1].Index, "Soft Mask/");
        }

        static readonly Regex PropertiesPattern = new Regex(
            @"Properties\s*\{" +
                @"(?:[^{}]|(?<open>\{)|(?<-open>\}))*" + // Swallow all the content with balancing
                @"(?(open)(?!))" + // Match only if braces are balanced
                @"()$" + // Capture a point on the line end of last property
            @"(\s*)\}", // Capture whitespace before ending } to keep indent
            RegexOptions.Multiline);

        static string InjectProperty(string shader) {
            var match = EnsureMatch(PropertiesPattern, shader, "Unable to inject Soft Mask property");
            var endOfLastProperty = match.Groups[1].Index;
            var padding = match.Groups[2].Value;
            return shader.Insert(endOfLastProperty,
                "\n" + padding + "\t_SoftMask(\"Mask\", 2D) = \"white\" {} // Soft Mask");
        }

        static readonly Regex LastPragmaPattern = LastDirectivePattern("pragma");
        static readonly Regex LastIncludePattern = LastDirectivePattern("include");

        static Regex LastDirectivePattern(string directive) {
            return new Regex(
                @"^([ \t]*)#" + directive + @"[^\n]*()$",
                RegexOptions.Multiline);
        }

        static string InjectPragma(string shader) {
            var match = EnsureLastMatch(LastPragmaPattern, shader, "Unable to inject Soft Mask #pragma");
            var padding = match.Groups[1].Value;
            return shader.Insert(match.Groups[2].Index,
                "\n" +
                "\n" + padding + "// Soft Mask" +
                "\n" + padding + "#pragma multi_compile __ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED\n");
        }

        static string InjectInclude(string shader) {
            var match = EnsureLastMatch(LastIncludePattern, shader, "Unable to inject Soft Mask #include");
            var padding = match.Groups[1].Value;
            return shader.Insert(match.Groups[2].Index,
                "\n" + padding + "#include \"SoftMask.cginc\" // Soft Mask");
        }

        static readonly Regex TexcoordPattern = new Regex(@"TEXCOORD(\d+)");

        static string InjectV2FFields(string shader, string v2fStructName) {
            var pattern = new Regex(
                @"struct\s+" + v2fStructName + @"\s*\{[^}]*()$(\s*)\}",  // Do not balance braces -
                RegexOptions.Multiline);                                 // expecting no braces inside struct
            var match = EnsureMatch(pattern, shader, "Unable to inject Soft Mask V2F signature");
            var texCoords = TexcoordPattern.Matches(match.Value);
            var maxUsedTexCoord =
                texCoords.OfType<Match>()
                    .Where(m => m.Success)
                    .Max(m => int.Parse(m.Groups[1].Value));
            var padding = match.Groups[2].Value;
            return shader.Insert(match.Groups[1].Index,
                "\n" + padding + "\tSOFTMASK_COORDS(" + (maxUsedTexCoord + 1) + ") // Soft Mask");
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

        static readonly Regex ReturnPattern = new Regex(@"^([^\n]*)return\s+([^;]+);\s*$", RegexOptions.Multiline);

        static string InjectVertexInstructions(string shader, string v2fStruct, string v2fPositionField, string function) {
            return ModifyFunctionReturn(shader, v2fStruct, function, (varName, inputName) =>
                "SOFTMASK_CALCULATE_COORDS(" + varName + ", " + inputName + "." + v2fPositionField + ")"
            );
        }

        static string InjectFragmentInstructions(string shader, BlendingMode blendingMode, string function) {
            var maskedComponent = blendingMode == BlendingMode.Classic ? ".a" : "";
            return ModifyFunctionReturn(shader, @"fixed4", function, (varName, inputName) =>
                varName + maskedComponent + " *= SOFTMASK_GET_MASK(" + inputName + ");"
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
            var returnMatch = EnsureMatch(ReturnPattern, functionBody, "Unable to find vertex shader's return statement");
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
