using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class AutomatedTestsRunnerUI : MonoBehaviour {
        public AutomatedTestsRunner testsRunner;
        [Space]
        public GameObject uiRoot;
        public Text statusText;
        public Text errorsText;
        public RawImage errorDiffImage;

        public IEnumerator Start() {
            uiRoot.SetActive(false);
            yield return new WaitUntil(() => testsRunner.isFinished);
            uiRoot.SetActive(true);
            statusText.text = "Finished";
            errorsText.text = FormatErrors();
            errorDiffImage.texture = DiffTexture();
        }

        string FormatErrors() {
            var output = new StringBuilder();
            foreach (var kv in testsRunner.testResults) {
                var result = kv.Value;
                if (result.isFail) {
                    output.AppendFormat("{0}: FAIL", kv.Key);
                    output.AppendLine();
                    foreach (var err in result.errors) {
                        output.Append("  ");
                        output.AppendLine(err.message);
                    }
                }
            }
            foreach (var kv in testsRunner.testResults) {
                var result = kv.Value;
                if (result.isPass) {
                    output.AppendFormat("{0}: OK", kv.Key);
                    output.AppendLine();
                }
            }
            return output.ToString();
        }

        Texture DiffTexture() {
            var firstFailed = testsRunner.testResults.Values.FirstOrDefault(x => x.isFail);
            var firstError = firstFailed != null ? firstFailed.errors.First() : null;
            return firstError != null ? firstError.diff : null;
        }
    }
}
