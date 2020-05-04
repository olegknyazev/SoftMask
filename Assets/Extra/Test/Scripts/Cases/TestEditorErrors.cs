using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SoftMasking.Tests {
    public class TestEditorErrors : MonoBehaviour {
        [Serializable] public class Case {
            public GameObject objectToActivate;
            AutomatedTest _test;

            public virtual IEnumerable Execute(AutomatedTest test) {
                _test = test;
                try {
                    foreach (var step in DoExecute())
                        yield return step;
                } finally {
                    _test = null;
                }
            }

            protected virtual IEnumerable DoExecute() {
                yield return Proceed();
                objectToActivate.SetActive(true);
                yield return Proceed();
            }

            protected YieldInstruction Proceed() {
                return _test.Proceed(0.1f);
            }
        }

        [Serializable] public class UnsupportedShaderCase : Case {
            public Image badImage;

            protected override IEnumerable DoExecute() {
                yield return Proceed();
                objectToActivate.SetActive(true);
                yield return Proceed();
                var prevMaterial = badImage.material;
                badImage.material = null;
                yield return Proceed();
                badImage.material = prevMaterial;
                yield return Proceed();
            }
        }

        public AutomatedTest automatedTest;
        public Case[] cases;
        public UnsupportedShaderCase unsupportedShaderCase;

        public IEnumerator Start() {
            var casesToRun = new [] { unsupportedShaderCase }.Concat(cases);
            foreach (var c in casesToRun)
                foreach (var step in c.Execute(automatedTest))
                    yield return step;
            yield return automatedTest.Finish();
        }
    }
}
