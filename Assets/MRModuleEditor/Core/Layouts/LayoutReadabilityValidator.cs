using System;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Validation;

namespace MRModuleEditor.Core.Layouts
{
    public static class LayoutReadabilityValidator
    {
        private const float MinimumComfortScale = 0.1f;
        private const float MaximumComfortScale = 2.5f;
        private const float MaximumHeadHorizontalOffset = 1.2f;
        private const float MinimumHeadVerticalOffset = -1.15f;
        private const float MaximumHeadVerticalOffset = 0.85f;
        private const float MaximumHeadExtraDepth = 0.5f;
        private const float MaximumWorldDistance = 4.5f;
        private const float MinimumWorldHeight = -2.25f;
        private const float MaximumWorldHeight = 2.5f;
        private const float MaximumObjectCalloutDistance = 2f;
        private const float MinimumObjectCalloutDistance = 0.05f;

        public static void AddIssues(ModuleDocument document, List<ValidationIssue> issues)
        {
            if (document == null || issues == null || document.layouts == null)
            {
                return;
            }

            Dictionary<string, AnchorDefinition> anchorsById = BuildAnchorsById(document);
            AddDuplicateTargetWarnings(document, issues);

            for (int i = 0; i < document.layouts.Count; i++)
            {
                LayoutDefinition layout = document.layouts[i];
                if (layout == null)
                {
                    continue;
                }

                string location = "layouts[" + i + "]";
                AddScaleIssues(layout, location, issues);
                AddProfileIssues(layout, location, issues);
                AddAnchorSpecificReadabilityIssues(layout, anchorsById, location, issues);
            }
        }

        private static void AddDuplicateTargetWarnings(ModuleDocument document, List<ValidationIssue> issues)
        {
            Dictionary<string, int> firstIndexByTarget = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < document.layouts.Count; i++)
            {
                LayoutDefinition layout = document.layouts[i];
                if (layout == null || string.IsNullOrWhiteSpace(layout.targetId))
                {
                    continue;
                }

                int firstIndex;
                if (!firstIndexByTarget.TryGetValue(layout.targetId, out firstIndex))
                {
                    firstIndexByTarget.Add(layout.targetId, i);
                    continue;
                }

                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.targetId.duplicate",
                    "More than one layout targets '" + layout.targetId + "'. The panel resolver uses the first matching layout, and object layouts can overwrite each other in list order. First duplicate target was at layouts[" + firstIndex + "].",
                    "layouts[" + i + "]"));
            }
        }

        private static void AddScaleIssues(LayoutDefinition layout, string location, List<ValidationIssue> issues)
        {
            Vector3Data scale = layout.scale ?? new Vector3Data(1f, 1f, 1f);
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "layout.scale.nonPositive",
                    "Layout scale must be positive on every axis. Use 1,1,1 for normal size.",
                    location));
                return;
            }

            if (scale.x < MinimumComfortScale || scale.y < MinimumComfortScale || scale.z < MinimumComfortScale)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.scale.tooSmall",
                    "Layout scale is very small. It may be hard to read or select in headset preview.",
                    location));
            }

            if (scale.x > MaximumComfortScale || scale.y > MaximumComfortScale || scale.z > MaximumComfortScale)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.scale.tooLarge",
                    "Layout scale is very large. It may dominate the scene or be uncomfortable at close range.",
                    location));
            }
        }

        private static void AddProfileIssues(LayoutDefinition layout, string location, List<ValidationIssue> issues)
        {
            if (layout == null)
            {
                return;
            }

            if (layout.readabilityProfile == LayoutReadabilityProfiles.HeadPanel && !layout.faceUser)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.headPanel.faceUser",
                    "Head panel readability profile usually works best with faceUser enabled.",
                    location));
            }

            if (layout.readabilityProfile == LayoutReadabilityProfiles.ObjectCallout
                && LayoutFollowModes.Normalize(layout.followMode) == LayoutFollowModes.Fixed)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.objectCallout.follow",
                    "Object callout readability profile usually works best with followAnchor or smoothFollow so the label stays attached while the object moves.",
                    location));
            }
        }

        private static void AddAnchorSpecificReadabilityIssues(
            LayoutDefinition layout,
            Dictionary<string, AnchorDefinition> anchorsById,
            string location,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(layout.anchorId) || anchorsById == null)
            {
                return;
            }

            AnchorDefinition anchor;
            if (!anchorsById.TryGetValue(layout.anchorId, out anchor) || anchor == null)
            {
                return;
            }

            Vector3Data position = layout.position ?? new Vector3Data();
            if (anchor.type == "head")
            {
                AddHeadAnchorIssues(layout, position, location, issues);
            }
            else if (anchor.type == "world")
            {
                AddWorldAnchorIssues(layout, position, location, issues);
            }
            else if (anchor.type == "object")
            {
                AddObjectAnchorIssues(layout, anchor, position, location, issues);
            }

            AddFaceUserRecommendation(layout, anchor, location, issues);
        }

        private static void AddHeadAnchorIssues(
            LayoutDefinition layout,
            Vector3Data position,
            string location,
            List<ValidationIssue> issues)
        {
            if (Math.Abs(position.x) > MaximumHeadHorizontalOffset
                || position.y < MinimumHeadVerticalOffset
                || position.y > MaximumHeadVerticalOffset)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.headOffset.comfort",
                    "Head-anchored layout is far from the comfortable central view. Prefer modest x/y offsets and keep the panel easy to find.",
                    location));
            }

            if (Math.Abs(position.z) > MaximumHeadExtraDepth)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.headOffset.extraDepth",
                    "Head-anchored layout has a large z offset. AnchorResolver already supplies forward distance, so z usually stays near 0.",
                    location));
            }
        }

        private static void AddWorldAnchorIssues(LayoutDefinition layout, Vector3Data position, string location, List<ValidationIssue> issues)
        {
            float distance = Length(position);
            if (distance > MaximumWorldDistance)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.worldDistance",
                    "World-anchored layout is far from the world origin. Recenter may not make this target comfortable for repeated demos.",
                    location));
            }

            if (layout != null && layout.visibilityRange > 0f && distance > layout.visibilityRange)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.visibilityRange",
                    "Layout position is outside its visibilityRange. Increase the range, move the layout closer, or set visibilityRange to 0 for unlimited.",
                    location));
            }

            if (position.y < MinimumWorldHeight || position.y > MaximumWorldHeight)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.worldHeight",
                    "World-anchored layout height is unusual. Check the object or panel in headset preview.",
                    location));
            }
        }

        private static void AddObjectAnchorIssues(
            LayoutDefinition layout,
            AnchorDefinition anchor,
            Vector3Data position,
            string location,
            List<ValidationIssue> issues)
        {
            float distance = Length(position);
            bool targetIsStep = !string.IsNullOrWhiteSpace(layout.targetId) && layout.targetId.StartsWith("step.", StringComparison.Ordinal);
            bool targetIsObject = !string.IsNullOrWhiteSpace(layout.targetId) && layout.targetId.StartsWith("object.", StringComparison.Ordinal);

            if (targetIsStep && distance < MinimumObjectCalloutDistance)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.objectCallout.overlap",
                    "Object-anchored step layout is almost exactly at the object origin. Add a small local offset so the callout does not overlap the object.",
                    location));
            }

            if (distance > MaximumObjectCalloutDistance)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.objectCallout.distance",
                    "Object-anchored layout is far from its target object. Keep callouts close enough that their relationship is obvious.",
                    location));
            }

            if (targetIsObject && string.Equals(layout.targetId, anchor.targetObjectId, StringComparison.Ordinal))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.readability.objectAnchor.selfTarget",
                    "Object layout is relative to an anchor that targets the same object. Prefer a world anchor for object placement to avoid confusing self-relative transforms.",
                    location));
            }
        }

        private static void AddFaceUserRecommendation(
            LayoutDefinition layout,
            AnchorDefinition anchor,
            string location,
            List<ValidationIssue> issues)
        {
            if (layout == null || anchor == null)
            {
                return;
            }

            bool targetIsStep = !string.IsNullOrWhiteSpace(layout.targetId) && layout.targetId.StartsWith("step.", StringComparison.Ordinal);
            if (!targetIsStep)
            {
                return;
            }

            if ((anchor.type == "world" || anchor.type == "object") && !layout.faceUser)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "layout.faceUser.recommended",
                    "World/object anchored step panels and callouts are usually easier to read with faceUser enabled.",
                    location));
            }
        }

        private static Dictionary<string, AnchorDefinition> BuildAnchorsById(ModuleDocument document)
        {
            Dictionary<string, AnchorDefinition> result = new Dictionary<string, AnchorDefinition>(StringComparer.Ordinal);
            if (document == null || document.anchors == null)
            {
                return result;
            }

            for (int i = 0; i < document.anchors.Count; i++)
            {
                AnchorDefinition anchor = document.anchors[i];
                if (anchor == null || string.IsNullOrWhiteSpace(anchor.id) || result.ContainsKey(anchor.id))
                {
                    continue;
                }

                result.Add(anchor.id, anchor);
            }

            return result;
        }

        private static float Length(Vector3Data value)
        {
            if (value == null)
            {
                return 0f;
            }

            return (float)Math.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z);
        }
    }
}
