using System.Collections.Generic;
using System.IO;
using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using MRModuleEditor.Core.Validation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class ModuleValidatorTests
    {
        private static string SamplePath
        {
            get
            {
                return Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Assets",
                    ModuleFilePaths.SampleModuleRelativePathFromAssets);
            }
        }

        [Test]
        public void ValidSampleModule_HasNoValidationErrors()
        {
            ModuleDocument document = ModuleJsonSerializer.LoadFromFile(SamplePath);
            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsFalse(
                issues.Any(issue => issue.severity == ValidationSeverity.Error),
                string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()));
        }

        [Test]
        public void MissingTitle_ReportsError()
        {
            ModuleDocument document = MakeValidModuleInMemory();
            document.title = "";

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "document.title"));
        }

        [Test]
        public void DuplicateIds_ReportError()
        {
            ModuleDocument document = MakeValidModuleInMemory();
            document.steps[1].id = document.steps[0].id;

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "id.duplicate"));
        }

        [Test]
        public void UnknownStepType_ReportsError()
        {
            ModuleDocument document = MakeValidModuleInMemory();
            document.steps[0].type = "teleportRobotThroughWall";

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "step.unknownType"));
        }

        [Test]
        public void MoveObjectWithoutTarget_ReportsError()
        {
            ModuleDocument document = MakeValidModuleInMemory();
            document.steps[1].type = "moveObject";
            document.steps[1].parameters.Remove("objectId");

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "step.objectId.missing"));
        }

        [Test]
        public void McqWithoutChoices_ReportsError()
        {
            ModuleDocument document = MakeValidModuleInMemory();
            ModuleStep mcq = document.steps[2];
            mcq.parameters["choices"] = new JArray();

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "mcq.choices.empty"));
        }

        private static ModuleDocument MakeValidModuleInMemory()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.test";
            document.title = "Test Module";
            document.description = "A test module.";
            document.author = "Tests";
            document.estimatedDurationSeconds = 60;

            document.objects.Add(new ModuleObject
            {
                id = "object.robot_preview",
                label = "Robot Preview",
                bindingKey = "RobotPreview"
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.head.default",
                type = "head"
            });

            ModuleStep text = new ModuleStep
            {
                id = "step.001",
                type = "text",
                title = "Welcome",
                durationSeconds = 5
            };
            text.parameters["text"] = JToken.FromObject("Hello.");
            text.parameters["anchorId"] = JToken.FromObject("anchor.head.default");

            ModuleStep showObject = new ModuleStep
            {
                id = "step.002",
                type = "showObject",
                title = "Show Object"
            };
            showObject.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            showObject.parameters["visible"] = JToken.FromObject(true);

            ModuleStep mcq = new ModuleStep
            {
                id = "step.003",
                type = "mcq",
                title = "Question"
            };
            mcq.parameters["question"] = JToken.FromObject("What is tested?");
            mcq.parameters["choices"] = JToken.FromObject(new[] { "A", "B" });
            mcq.parameters["correctIndex"] = JToken.FromObject(0);

            document.steps.Add(text);
            document.steps.Add(showObject);
            document.steps.Add(mcq);

            return document;
        }
    }
}