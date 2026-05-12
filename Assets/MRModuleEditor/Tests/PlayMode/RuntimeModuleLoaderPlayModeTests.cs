using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MRModuleEditor.Core.Validation;
using MRModuleEditor.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class RuntimeModuleLoaderPlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeModuleLoader_LoadsDefaultSampleModuleWithoutErrors()
        {
            GameObject gameObject = new GameObject("RuntimeModuleLoader Test Object");
            RuntimeModuleLoader loader = gameObject.AddComponent<RuntimeModuleLoader>();

            bool ok = loader.LoadAndValidate();

            Assert.IsTrue(ok, FormatIssues(loader.LastIssues));
            Assert.IsNotNull(loader.LoadedModule);
            Assert.AreEqual("Forward Kinematics Mini Demo", loader.LoadedModule.title);
            Assert.IsFalse(loader.LastIssues.Any(issue => issue.severity == ValidationSeverity.Error));

            Object.Destroy(gameObject);
            yield return null;
        }

        private static string FormatIssues(IEnumerable<ValidationIssue> issues)
        {
            if (issues == null)
            {
                return "No issues list was returned.";
            }

            return string.Join("\n", issues.Select(issue => issue.ToString()).ToArray());
        }
    }
}