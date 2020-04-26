using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class AutomatedTestsRunnerUI : MonoBehaviour {
        public AutomatedTestsRunner testsRunner;
        [Space]
        public GameObject[] activateAfterTest;
        public Text statusText;
        public Graphic statusColorDisplay;
        public GameObject detailsRoot;
        public Text errorsText;
        public RawImage errorDiffImage;

        public IEnumerator Start() {
            foreach (var obj in activateAfterTest)
                obj.SetActive(false);
            yield return new WaitUntil(() => testsRunner.isFinished);
            foreach (var obj in activateAfterTest)
                obj.SetActive(true);
            statusText.text = FormatStatus();
            var isFail = testsRunner.testResults.Count(x => x.isFail) > 0;
            detailsRoot.SetActive(isFail);
            statusColorDisplay.color = isFail ? Color.red : Color.green;
            if (isFail) {
                errorsText.text = FormatErrors();
                errorDiffImage.texture = DiffTexture();
                errorDiffImage.SetNativeSize();
            }
        }

        string FormatStatus() {
            var testsFailed = testsRunner.testResults.Count(x => x.isFail);
            return testsFailed > 0
                ? "FAIL"
                : string.Format("PASS ({0} tests runned)", testsRunner.testResults.Count);
        }

        string FormatErrors() {
            var output = new StringBuilder();
            foreach (var result in testsRunner.testResults)
                if (result.isFail) {
                    output.AppendFormat("{0}: FAIL", result.sceneName);
                    output.AppendLine();
                    foreach (var err in result.errors) {
                        output.Append("  ");
                        output.AppendLine(err.message);
                    }
                }
            return output.ToString();
        }

        Texture DiffTexture() {
            var firstFailed = testsRunner.testResults.FirstOrDefault(x => x.isFail);
            var firstError = firstFailed != null ? firstFailed.errors.First() : null;
            return firstError != null ? firstError.diff : null;
        }
    }
}
