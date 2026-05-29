using System.Collections.Generic;
using System.IO;
using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using MRModuleEditor.Core.Templates;
using MRModuleEditor.Core.Validation;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class ModuleTemplateFactoryTests
    {
        [Test]
        public void ForwardKinematicsMiniTemplate_ValidatesWithoutErrors()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsFalse(
                ModuleValidator.HasError(issues),
                string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()));
        }

        [Test]
        public void ForwardKinematicsMiniTemplate_RoundTripsThroughJson()
        {
            ModuleDocument original = ModuleTemplateFactory.CreateForwardKinematicsMini();
            string json = ModuleJsonSerializer.Serialize(original);
            ModuleDocument copy = ModuleJsonSerializer.Deserialize(json);

            Assert.AreEqual(original.moduleId, copy.moduleId);
            Assert.AreEqual(original.title, copy.title);
            Assert.AreEqual(original.steps.Count, copy.steps.Count);
            Assert.IsTrue(copy.steps.Any(step => step != null && step.type == "mcq"));
            Assert.IsTrue(copy.steps.Any(step => step != null && step.type == "resetRobot"));
        }

        [Test]
        public void GenericBlankLessonTemplate_ValidatesWithoutErrors()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateGenericBlankLesson();
            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsFalse(
                ModuleValidator.HasError(issues),
                string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()));
            Assert.IsTrue(document.steps.Any(step => step != null && step.type == "confirm"));
            Assert.IsTrue(document.steps.Any(step => step != null && step.type == "mcq"));
        }

        [Test]
        public void EquipmentOrientationTemplate_UsesGenericObjectFeatures()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateEquipmentOrientationMini();
            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.IsFalse(
                ModuleValidator.HasError(issues),
                string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()));
            Assert.IsTrue(document.steps.Any(step => step != null && step.type == "highlightObject"));
            Assert.IsTrue(document.steps.Any(step => step != null && step.type == "showCallout"));
            Assert.IsTrue(document.layouts.Any(layout => layout != null && layout.targetId == "object.equipment_demo"));
        }

        [Test]
        public void EmptyTemplate_HasRequiredDefaultAnchors()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateEmptyModule();

            Assert.IsTrue(document.anchors.Any(anchor => anchor.id == "anchor.head.default"));
            Assert.IsTrue(document.anchors.Any(anchor => anchor.id == "anchor.world.table"));
        }
    }
}