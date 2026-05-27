using System.IO;
using System.Linq;
using MRModuleEditor.Core.Serialization;
using MRModuleEditor.Core.Validation;
using NUnit.Framework;
using UnityEngine;

namespace MRModuleEditor.Tests.EditMode
{
    public class BaselineSmokeTests
    {
        [Test]
        public void BaselineSampleModule_ExistsAndValidatesWithoutErrors()
        {
            string modulePath = Path.Combine(
                Application.dataPath,
                ModuleFilePaths.SampleModuleRelativePathFromAssets);

            Assert.That(
                File.Exists(modulePath),
                Is.True,
                "Expected baseline sample module at: " + modulePath);

            var document = ModuleJsonSerializer.LoadFromFile(modulePath);
            var errors = ModuleValidator
                .Validate(document)
                .Where(issue => issue.severity == ValidationSeverity.Error)
                .Select(issue => issue.ToString())
                .ToArray();

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void RequiredDocs_Exist()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string docsRoot = Path.Combine(projectRoot, "Assets/MRModuleEditor/Docs");

            string[] requiredDocs =
            {
                "README.md",
                "QUICKSTART.md",
                "ARCHITECTURE.md",
                "MODULE_SCHEMA.md",
                "AUTHORING_GUIDE.md",
                "RUNTIME_SCENE_SETUP.md",
                "ADDING_STEP_TYPES_CURRENT.md",
                "TESTING.md",
                "TROUBLESHOOTING.md",
                "DEMO_RUNBOOK.md",
                "KNOWN_LIMITATIONS.md",
                "ROADMAP.md",
                Path.Combine("ADR", "0001-json-module-document.md"),
                Path.Combine("ADR", "0002-step-type-catalog.md"),
                Path.Combine("ADR", "0003-anchor-layout-data-model.md")
            };

            foreach (string relativePath in requiredDocs)
            {
                string fullPath = Path.Combine(docsRoot, relativePath);
                Assert.That(
                    File.Exists(fullPath),
                    Is.True,
                    "Missing required baseline doc: " + fullPath);
            }
        }
    }
}

