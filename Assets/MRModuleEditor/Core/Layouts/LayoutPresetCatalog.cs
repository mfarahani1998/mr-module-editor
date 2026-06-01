using System;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Core.Layouts
{
    public static class LayoutPresetCatalog
    {
        public const string StepTargetKind = "step";
        public const string ObjectTargetKind = "object";

        private static readonly LayoutPresetDefinition[] Presets =
        {
            new LayoutPresetDefinition(
                "head.panel.center",
                "Head Panel Center",
                "Readable default for text, confirm, image, and MCQ panels. The head anchor already supplies forward distance, so z stays at 0.",
                StepTargetKind,
                "head",
                Vec(0f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f),
                true,
                LayoutFollowModes.SmoothFollow,
                3.5f,
                LayoutReadabilityProfiles.HeadPanel,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "head.panel.lower",
                "Head Panel Lower",
                "Places a panel lower in view without pushing it farther away.",
                StepTargetKind,
                "head",
                Vec(0f, -0.45f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f),
                true,
                LayoutFollowModes.SmoothFollow,
                3.5f,
                LayoutReadabilityProfiles.HeadPanel,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "head.panel.left",
                "Head Panel Left",
                "Useful when the main object is centered and the instruction panel should sit to the learner's left.",
                StepTargetKind,
                "head",
                Vec(-0.55f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f),
                true,
                LayoutFollowModes.SmoothFollow,
                3.5f,
                LayoutReadabilityProfiles.HeadPanel,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "head.panel.right",
                "Head Panel Right",
                "Useful when the main object is centered and the instruction panel should sit to the learner's right.",
                StepTargetKind,
                "head",
                Vec(0.55f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f),
                true,
                LayoutFollowModes.SmoothFollow,
                3.5f,
                LayoutReadabilityProfiles.HeadPanel,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "world.panel.demo",
                "World Panel Demo",
                "Places a world-anchored panel above the simulator/world origin for stable demo narration.",
                StepTargetKind,
                "world",
                Vec(0f, 1.25f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f),
                true,
                LayoutFollowModes.Fixed,
                4.5f,
                LayoutReadabilityProfiles.WorldPanel,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "world.panel.side",
                "World Panel Side",
                "Places a stable panel to one side of the demo area.",
                StepTargetKind,
                "world",
                Vec(-0.75f, 1.1f, 0.35f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f),
                true,
                LayoutFollowModes.Fixed,
                4.5f,
                LayoutReadabilityProfiles.WorldPanel,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "object.callout.above",
                "Object Callout Above",
                "Small callout attached above a bound object.",
                StepTargetKind,
                "object",
                Vec(0f, 0.7f, 0f),
                Vec(0f, 0f, 0f),
                Vec(0.75f, 0.75f, 0.75f),
                true,
                LayoutFollowModes.FollowAnchor,
                2.5f,
                LayoutReadabilityProfiles.ObjectCallout,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "object.callout.side",
                "Object Callout Side",
                "Small callout attached beside a bound object.",
                StepTargetKind,
                "object",
                Vec(0.45f, 0.45f, 0f),
                Vec(0f, 0f, 0f),
                Vec(0.75f, 0.75f, 0.75f),
                true,
                LayoutFollowModes.FollowAnchor,
                2.5f,
                LayoutReadabilityProfiles.ObjectCallout,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "world.object.center",
                "Object Demo Center",
                "Places a scene-bound object in the central demo area in front of the world origin.",
                ObjectTargetKind,
                "world",
                Vec(0f, 0f, 1.5f),
                Vec(0f, 0f, 0f),
                Vec(0.5f, 0.5f, 0.5f),
                false,
                LayoutFollowModes.Fixed,
                0f,
                LayoutReadabilityProfiles.WorldObject,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "world.object.left",
                "Object Demo Left",
                "Places a scene-bound object to the left side of the demo area.",
                ObjectTargetKind,
                "world",
                Vec(-0.65f, 0f, 1.45f),
                Vec(0f, 0f, 0f),
                Vec(0.5f, 0.5f, 0.5f),
                false,
                LayoutFollowModes.Fixed,
                0f,
                LayoutReadabilityProfiles.WorldObject,
                LayoutDeviceProfiles.Any),

            new LayoutPresetDefinition(
                "world.object.right",
                "Object Demo Right",
                "Places a scene-bound object to the right side of the demo area.",
                ObjectTargetKind,
                "world",
                Vec(0.65f, 0f, 1.45f),
                Vec(0f, 0f, 0f),
                Vec(0.5f, 0.5f, 0.5f),
                false,
                LayoutFollowModes.Fixed,
                0f,
                LayoutReadabilityProfiles.WorldObject,
                LayoutDeviceProfiles.Any)
        };

        public static LayoutPresetDefinition[] GetPresets(string targetKind, string anchorType)
        {
            List<LayoutPresetDefinition> result = new List<LayoutPresetDefinition>();
            string normalizedTargetKind = targetKind ?? "";
            string normalizedAnchorType = anchorType ?? "";

            for (int i = 0; i < Presets.Length; i++)
            {
                LayoutPresetDefinition preset = Presets[i];
                if (preset == null)
                {
                    continue;
                }

                if (!string.Equals(preset.targetKind, normalizedTargetKind, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(preset.anchorType, normalizedAnchorType, StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(preset.Clone());
            }

            return result.ToArray();
        }

        public static LayoutPresetDefinition Find(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return null;
            }

            for (int i = 0; i < Presets.Length; i++)
            {
                LayoutPresetDefinition preset = Presets[i];
                if (preset != null && string.Equals(preset.id, presetId, StringComparison.Ordinal))
                {
                    return preset.Clone();
                }
            }

            return null;
        }

        public static bool TryApplyPreset(LayoutDefinition layout, string presetId)
        {
            LayoutPresetDefinition preset = Find(presetId);
            if (preset == null)
            {
                return false;
            }

            ApplyPreset(layout, preset);
            return true;
        }

        public static void ApplyPreset(LayoutDefinition layout, LayoutPresetDefinition preset)
        {
            if (layout == null || preset == null)
            {
                return;
            }

            layout.position = LayoutPresetDefinition.Clone(preset.position, new Vector3Data());
            layout.rotationEuler = LayoutPresetDefinition.Clone(preset.rotationEuler, new Vector3Data());
            layout.scale = LayoutPresetDefinition.Clone(preset.scale, new Vector3Data(1f, 1f, 1f));
            layout.faceUser = preset.faceUser;
            layout.followMode = preset.followMode;
            layout.visibilityRange = preset.visibilityRange;
            layout.readabilityProfile = preset.readabilityProfile;
            layout.deviceProfile = preset.deviceProfile;
        }

        private static Vector3Data Vec(float x, float y, float z)
        {
            return new Vector3Data(x, y, z);
        }
    }
}
