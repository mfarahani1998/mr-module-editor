using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using MRModuleEditor.Core.Validation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class Phase3GenericStepDefinitionsTests
    {
        [Test]
        public void CatalogContainsPhase3GenericSteps()
        {
            AssertDefinition("highlightObject", "Objects");
            AssertDefinition("showCallout", "Objects");
            AssertDefinition("setVariable", "Flow");
            AssertDefinition("confirm", "Flow");

            StepTypeDefinition removedDefinition;
            Assert.IsFalse(StepCatalog.Global.TryGet("waitForSignal", out removedDefinition));
        }

        [Test]
        public void ConfirmStepExposesSignalCompletionAsOptInParameters()
        {
            StepTypeDefinition definition;
            Assert.IsTrue(StepCatalog.Global.TryGet("confirm", out definition));

            StepParameterDefinition signalAction = definition.Parameters.First(parameter => parameter.Key == "signalAction");
            StepParameterDefinition signalTarget = definition.Parameters.First(parameter => parameter.Key == "signalTargetId");
            StepParameterDefinition signalPayload = definition.Parameters.First(parameter => parameter.Key == "signalIntPayload");

            Assert.AreEqual("Select", signalAction.DefaultValue.Value<string>());
            Assert.AreEqual("{stepId}.confirm", signalTarget.DefaultValue.Value<string>());
            Assert.AreEqual(-1, signalPayload.DefaultValue.Value<int>());
            Assert.IsTrue(signalAction.HasVisibilityCondition);
            Assert.AreEqual("completeOnSignal", signalAction.VisibleWhenParameterKey);
            Assert.IsTrue(signalAction.VisibleWhenBoolValue);

            ModuleStep step = new ModuleStep
            {
                id = "step.confirmSignal",
                type = "confirm"
            };

            StepCatalog.Global.ApplyDefaults(step);

            Assert.IsFalse(step.GetBool("completeOnSignal", true));
            Assert.IsFalse(step.HasParameter("signalAction"));
            Assert.IsFalse(step.HasParameter("signalTargetId"));
            Assert.IsFalse(step.HasParameter("signalIntPayload"));
        }

        [Test]
        public void HighlightObjectValidationUsesCatalogObjectReference()
        {
            ModuleDocument document = MakeBaseDocument();
            ModuleStep step = new ModuleStep
            {
                id = "step.highlight",
                type = "highlightObject",
                title = "Highlight"
            };
            step.parameters["objectId"] = JToken.FromObject("object.missing");
            document.steps.Add(step);

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "step.objectId.unknown"));
        }

        [Test]
        public void SetVariableDefaultsAreAppliedFromCatalog()
        {
            ModuleStep step = new ModuleStep
            {
                id = "step.setVariable",
                type = "setVariable"
            };

            StepCatalog.Global.ApplyDefaults(step);

            Assert.AreEqual("Set Variable", step.title);
            Assert.AreEqual("String", step.GetString("valueType", ""));
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.GetString("variableKey", "")));
        }

        private static void AssertDefinition(string stepType, string category)
        {
            StepTypeDefinition definition;
            Assert.IsTrue(StepCatalog.Global.TryGet(stepType, out definition), stepType + " missing from catalog");
            Assert.AreEqual(category, definition.Category);
        }

        private static ModuleDocument MakeBaseDocument()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.phase3.validation";
            document.title = "Phase 3 Validation";
            document.author = "Tests";
            document.estimatedDurationSeconds = 5;
            document.objects.Add(new ModuleObject
            {
                id = "object.valid",
                bindingKey = "Valid Object"
            });
            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.head.default",
                type = "head"
            });
            return document;
        }
    }
}
