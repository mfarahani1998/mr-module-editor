using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Templates;
using MRModuleEditor.Core.Validation;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class ModuleLayoutValidationTests
    {
        [Test]
        public void ForwardKinematicsTemplate_HasPhaseDLayouts()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();

            Assert.IsTrue(document.layouts.Any(layout => layout.id == "layout.object.robot_world"));
            Assert.IsTrue(document.layouts.Any(layout => layout.anchorId == "anchor.head.default"));
            Assert.IsTrue(document.layouts.Any(layout => layout.anchorId == "anchor.object.robot"));
        }

        [Test]
        public void LayoutWithUnknownTarget_ReportsError()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            document.layouts.Add(new LayoutDefinition
            {
                id = "layout.bad_target",
                targetId = "step.does_not_exist",
                anchorId = "anchor.head.default"
            });

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "layout.targetId.unknown"));
        }

        [Test]
        public void LayoutWithUnknownAnchor_ReportsError()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            document.layouts.Add(new LayoutDefinition
            {
                id = "layout.bad_anchor",
                targetId = "step.001",
                anchorId = "anchor.does_not_exist"
            });

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "layout.anchorId.unknown"));
        }
    }
}