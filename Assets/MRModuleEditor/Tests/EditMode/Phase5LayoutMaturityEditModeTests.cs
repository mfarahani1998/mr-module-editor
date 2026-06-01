using System.Collections.Generic;
using System.Linq;
using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Validation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class Phase5LayoutMaturityEditModeTests
    {
        [Test]
        public void LayoutPresetCatalog_ReturnsHeadPanelPresets()
        {
            LayoutPresetDefinition[] presets = LayoutPresetCatalog.GetPresets(
                LayoutPresetCatalog.StepTargetKind,
                "head");

            Assert.That(presets.Any(preset => preset.id == "head.panel.center"), Is.True);
            Assert.That(presets.Any(preset => preset.id == "head.panel.left"), Is.True);
        }

        [Test]
        public void LayoutPresetCatalog_AppliesPresetWithoutChangingTargetOrAnchor()
        {
            LayoutDefinition layout = new LayoutDefinition
            {
                id = "layout.test",
                targetId = "step.001",
                anchorId = "anchor.head.default",
                position = new Vector3Data(9f, 9f, 9f),
                rotationEuler = new Vector3Data(9f, 9f, 9f),
                scale = new Vector3Data(9f, 9f, 9f)
            };

            bool applied = LayoutPresetCatalog.TryApplyPreset(layout, "head.panel.center");

            Assert.That(applied, Is.True);
            Assert.That(layout.targetId, Is.EqualTo("step.001"));
            Assert.That(layout.anchorId, Is.EqualTo("anchor.head.default"));
            Assert.That(layout.position.x, Is.EqualTo(0f));
            Assert.That(layout.position.y, Is.EqualTo(-0.15f));
            Assert.That(layout.position.z, Is.EqualTo(0f));
            Assert.That(layout.scale.x, Is.EqualTo(1f));
            Assert.That(layout.faceUser, Is.True);
            Assert.That(layout.followMode, Is.EqualTo(LayoutFollowModes.SmoothFollow));
            Assert.That(layout.readabilityProfile, Is.EqualTo(LayoutReadabilityProfiles.HeadPanel));
        }

        [Test]
        public void ModuleValidator_WarnsForDuplicateLayoutTarget()
        {
            ModuleDocument document = MakeValidDocumentWithOneLayout();
            document.layouts.Add(new LayoutDefinition
            {
                id = "layout.step.001.duplicate",
                targetId = "step.001",
                anchorId = "anchor.head.default",
                scale = new Vector3Data(1f, 1f, 1f)
            });

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.That(issues.Any(issue => issue.code == "layout.targetId.duplicate"), Is.True);
            Assert.That(ModuleValidator.HasError(issues), Is.False, string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()));
        }

        [Test]
        public void ModuleValidator_ErrorsForNonPositiveLayoutScale()
        {
            ModuleDocument document = MakeValidDocumentWithOneLayout();
            document.layouts[0].scale = new Vector3Data(1f, 0f, 1f);

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.That(issues.Any(issue => issue.code == "layout.scale.nonPositive"), Is.True);
            Assert.That(ModuleValidator.HasError(issues), Is.True);
        }

        [Test]
        public void ModuleValidator_WarnsForHeadLayoutExtraDepth()
        {
            ModuleDocument document = MakeValidDocumentWithOneLayout();
            document.layouts[0].position = new Vector3Data(0f, -0.15f, 1.2f);

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.That(issues.Any(issue => issue.code == "layout.readability.headOffset.extraDepth"), Is.True);
            Assert.That(ModuleValidator.HasError(issues), Is.False, string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()));
        }

        [Test]
        public void ModuleValidator_WarnsForCalibrationRequiredAnchorWithoutFallback()
        {
            ModuleDocument document = MakeValidDocumentWithOneLayout();
            document.anchors[0].calibrationRequired = true;
            document.anchors[0].fallbackAnchorId = "";

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.That(issues.Any(issue => issue.code == "anchor.fallbackAnchorId.recommended"), Is.True);
            Assert.That(ModuleValidator.HasError(issues), Is.False, string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()));
        }

        [Test]
        public void ModuleValidator_ErrorsForUnknownFollowMode()
        {
            ModuleDocument document = MakeValidDocumentWithOneLayout();
            document.layouts[0].followMode = "teleportToMars";

            List<ValidationIssue> issues = ModuleValidator.Validate(document);

            Assert.That(issues.Any(issue => issue.code == "layout.followMode.unknown"), Is.True);
            Assert.That(ModuleValidator.HasError(issues), Is.True);
        }


        private static ModuleDocument MakeValidDocumentWithOneLayout()
        {
            ModuleDocument document = new ModuleDocument
            {
                schemaVersion = "0.1",
                moduleId = "module.phase5.layout",
                title = "Phase 5 Layout Test",
                author = "Tests",
                estimatedDurationSeconds = 5
            };

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
                durationSeconds = 1f
            };
            text.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            text.parameters["text"] = JToken.FromObject("Hello.");
            document.steps.Add(text);

            document.layouts.Add(new LayoutDefinition
            {
                id = "layout.step.001.head_panel",
                targetId = "step.001",
                anchorId = "anchor.head.default",
                position = new Vector3Data(0f, -0.15f, 0f),
                rotationEuler = new Vector3Data(0f, 0f, 0f),
                scale = new Vector3Data(1f, 1f, 1f)
            });

            return document;
        }
    }
}
