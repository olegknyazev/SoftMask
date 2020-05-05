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
            UpdateStatus();
        }

        void UpdateStatus() {
            statusText.text = FormatStatus();
            detailsRoot.SetActive(isFail);
            statusColorDisplay.color = isFail ? Color.red : Color.green;
            if (isFail) {
                errorsText.text = FormatErrors();
                errorDiffImage.texture = DiffTexture();
                errorDiffImage.SetNativeSize();
            }
        }

        string FormatStatus() {
            return isFail
                ? "FAIL"
                : string.Format("PASS ({0} tests runned)", testResults.testCount);
        }

        bool isFail { get { return testResults.isFail; } }
        AutomatedTestResults testResults { get { return testsRunner.testResults; } }

        string FormatErrors() {
            var output = new StringBuilder();
            foreach (var result in testResults.failures) {
                output.AppendFormat("{0}: FAIL", result.sceneName);
                output.AppendLine();
                if (result.isFail) {
                    output.Append("  ");
                    output.AppendLine(result.error.message);
                }
            }
            return output.ToString();
        }

        Texture DiffTexture() {
            return this.testResults.failures.First().error.diff;
        }
    }
}
