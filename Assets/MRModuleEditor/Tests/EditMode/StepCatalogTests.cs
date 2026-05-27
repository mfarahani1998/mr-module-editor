using System.Collections.Generic;
using System.IO;
using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using MRModuleEditor.Core.Validation;
using MRModuleEditor.Domains.RoboticsLite;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class StepCatalogTests
    {
        [Test]
        public void BuiltInCatalog_ContainsConfirmStep()
        {
            StepTypeDefinition definition;
            Assert.IsTrue(StepCatalog.Global.TryGet("confirm", out definition));
            Assert.AreEqual("Flow", definition.Category);
        }

        [Test]
        public void ConfirmDefaults_AreAppliedFromCatalog()
        {
            ModuleStep step = new ModuleStep
            {
                id = "step.confirm",
                type = "confirm"
            };

            StepCatalog.Global.ApplyDefaults(step);

            Assert.AreEqual("Confirm", step.title);
            Assert.AreEqual("Continue", step.GetString("buttonLabel", ""));
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.GetString("message", "")));
        }

        [Test]
        public void ConfirmWithoutMessage_ReportsCatalogDrivenError()
        {
            ModuleDocument document = MakeConfirmModule();
            document.steps[0].parameters.Remove("message");

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "step.message.missing"));
        }

        [Test]
        public void RoboticsLiteDefinitions_CanRegisterWithoutCoreValidatorHardCoding()
        {
            RoboticsLiteStepDefinitions.Register();

            StepTypeDefinition definition;
            Assert.IsTrue(StepCatalog.Global.TryGet("rotateJoint", out definition));
            Assert.AreEqual("Robotics Lite", definition.Category);
        }

        [Test]
        public void CoreValidator_DoesNotHardCodeRoboticsLiteStepTypeNames()
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "MRModuleEditor",
                "Core",
                "Validation",
                "ModuleValidator.cs");

            string source = File.ReadAllText(path);

            Assert.IsFalse(source.Contains("rotateJoint"));
            Assert.IsFalse(source.Contains("showFrame"));
            Assert.IsFalse(source.Contains("resetRobot"));
        }

        private static ModuleDocument MakeConfirmModule()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.confirm_test";
            document.title = "Confirm Test";
            document.author = "Tests";
            document.estimatedDurationSeconds = 5;

            ModuleStep step = new ModuleStep
            {
                id = "step.001",
                type = "confirm",
                title = "Ready Check",
                durationSeconds = 0f
            };
            step.parameters["message"] = JToken.FromObject("Continue when ready.");
            step.parameters["buttonLabel"] = JToken.FromObject("Continue");

            document.steps.Add(step);
            return document;
        }
    }
}
