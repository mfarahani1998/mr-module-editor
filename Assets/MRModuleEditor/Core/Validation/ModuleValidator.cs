using System;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using Newtonsoft.Json.Linq;

namespace MRModuleEditor.Core.Validation
{
    public static class ModuleValidator
    {
        private static readonly HashSet<string> KnownStepTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "text",
            "image",
            "wait",
            "showObject",
            "moveObject",
            "mcq",
            "rotateJoint",
            "showFrame"
        };

        private static readonly HashSet<string> KnownAnchorTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "head",
            "world",
            "object"
        };

        public static List<ValidationIssue> Validate(ModuleDocument document)
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();

            if (document == null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "document.null",
                    "ModuleDocument is null."));
                return issues;
            }

            CheckRequiredString(document.schemaVersion, "document.schemaVersion", "Schema version is missing.", issues);
            CheckRequiredString(document.moduleId, "document.moduleId", "Module id is missing.", issues);
            CheckRequiredString(document.title, "document.title", "Module title is missing.", issues);

            Dictionary<string, string> seenIds = new Dictionary<string, string>();
            AddId(document.moduleId, "module", "document.moduleId", seenIds, issues);

            HashSet<string> objectIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> assetIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> anchorIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> layoutTargetIds = CollectLayoutTargetIds(document);
            HashSet<string> stepIds = CollectStepIds(document);

            ValidateAssets(document, assetIds, seenIds, issues);
            ValidateObjects(document, objectIds, seenIds, issues);
            ValidateAnchors(document, objectIds, anchorIds, seenIds, issues);
            ValidateLayouts(document, anchorIds, layoutTargetIds, seenIds, issues);
            ValidateSteps(document, objectIds, assetIds, anchorIds, stepIds, seenIds, issues);

            return issues;
        }

        public static bool HasError(List<ValidationIssue> issues)
        {
            if (issues == null)
            {
                return false;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].severity == ValidationSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateAssets(
            ModuleDocument document,
            HashSet<string> assetIds,
            Dictionary<string, string> seenIds,
            List<ValidationIssue> issues)
        {
            if (document.assets == null)
            {
                return;
            }

            for (int i = 0; i < document.assets.Count; i++)
            {
                ModuleAsset asset = document.assets[i];
                string location = "assets[" + i + "]";

                if (asset == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "asset.null", "Asset entry is null.", location));
                    continue;
                }

                CheckRequiredString(asset.id, "asset.id", "Asset id is missing.", location, issues);
                CheckRequiredString(asset.type, "asset.type", "Asset type is missing.", location, issues);
                CheckRequiredString(asset.path, "asset.path", "Asset path is missing.", location, issues);
                AddId(asset.id, "asset", location, seenIds, issues);

                if (!string.IsNullOrWhiteSpace(asset.id))
                {
                    assetIds.Add(asset.id);
                }
            }
        }

        private static void ValidateObjects(
            ModuleDocument document,
            HashSet<string> objectIds,
            Dictionary<string, string> seenIds,
            List<ValidationIssue> issues)
        {
            if (document.objects == null)
            {
                return;
            }

            for (int i = 0; i < document.objects.Count; i++)
            {
                ModuleObject moduleObject = document.objects[i];
                string location = "objects[" + i + "]";

                if (moduleObject == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "object.null", "Object entry is null.", location));
                    continue;
                }

                CheckRequiredString(moduleObject.id, "object.id", "Object id is missing.", location, issues);
                CheckRequiredString(moduleObject.bindingKey, "object.bindingKey", "Object bindingKey is missing.", location, issues);
                AddId(moduleObject.id, "object", location, seenIds, issues);

                if (!string.IsNullOrWhiteSpace(moduleObject.id))
                {
                    objectIds.Add(moduleObject.id);
                }
            }
        }

        private static void ValidateAnchors(
            ModuleDocument document,
            HashSet<string> objectIds,
            HashSet<string> anchorIds,
            Dictionary<string, string> seenIds,
            List<ValidationIssue> issues)
        {
            if (document.anchors == null)
            {
                return;
            }

            for (int i = 0; i < document.anchors.Count; i++)
            {
                AnchorDefinition anchor = document.anchors[i];
                string location = "anchors[" + i + "]";

                if (anchor == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "anchor.null", "Anchor entry is null.", location));
                    continue;
                }

                CheckRequiredString(anchor.id, "anchor.id", "Anchor id is missing.", location, issues);
                CheckRequiredString(anchor.type, "anchor.type", "Anchor type is missing.", location, issues);
                AddId(anchor.id, "anchor", location, seenIds, issues);

                if (!string.IsNullOrWhiteSpace(anchor.id))
                {
                    anchorIds.Add(anchor.id);
                }

                if (!string.IsNullOrWhiteSpace(anchor.type) && !KnownAnchorTypes.Contains(anchor.type))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "anchor.unknownType",
                        "Unknown anchor type '" + anchor.type + "'. Expected head, world, or object.",
                        location));
                }

                if (anchor.type == "object")
                {
                    if (string.IsNullOrWhiteSpace(anchor.targetObjectId))
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            "anchor.targetObjectId.missing",
                            "Object anchor is missing targetObjectId.",
                            location));
                    }
                    else if (!objectIds.Contains(anchor.targetObjectId))
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            "anchor.targetObjectId.unknown",
                            "Object anchor references unknown object id '" + anchor.targetObjectId + "'.",
                            location));
                    }
                }
            }
        }

        private static void ValidateLayouts(
            ModuleDocument document,
            HashSet<string> anchorIds,
            HashSet<string> layoutTargetIds,
            Dictionary<string, string> seenIds,
            List<ValidationIssue> issues)
        {
            if (document.layouts == null)
            {
                return;
            }

            for (int i = 0; i < document.layouts.Count; i++)
            {
                LayoutDefinition layout = document.layouts[i];
                string location = "layouts[" + i + "]";

                if (layout == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "layout.null", "Layout entry is null.", location));
                    continue;
                }

                CheckRequiredString(layout.id, "layout.id", "Layout id is missing.", location, issues);
                AddId(layout.id, "layout", location, seenIds, issues);

                if (!string.IsNullOrWhiteSpace(layout.anchorId) && !anchorIds.Contains(layout.anchorId))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "layout.anchorId.unknown",
                        "Layout references unknown anchor id '" + layout.anchorId + "'.",
                        location));
                }

                CheckRequiredString(layout.targetId, "layout.targetId", "Layout targetId is missing.", location, issues);

                if (!string.IsNullOrWhiteSpace(layout.targetId) && !layoutTargetIds.Contains(layout.targetId))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "layout.targetId.unknown",
                        "Layout references unknown target id '" + layout.targetId + "'.",
                        location));
                }
            }
        }

        private static void ValidateSteps(
            ModuleDocument document,
            HashSet<string> objectIds,
            HashSet<string> assetIds,
            HashSet<string> anchorIds,
            HashSet<string> stepIds,
            Dictionary<string, string> seenIds,
            List<ValidationIssue> issues)
        {
            if (document.steps == null || document.steps.Count == 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "steps.empty",
                    "Module must contain at least one step."));
                return;
            }

            for (int i = 0; i < document.steps.Count; i++)
            {
                ModuleStep step = document.steps[i];
                string location = "steps[" + i + "]";

                if (step == null)
                {
                    issues.Add(new ValidationIssue(ValidationSeverity.Error, "step.null", "Step entry is null.", location));
                    continue;
                }

                CheckRequiredString(step.id, "step.id", "Step id is missing.", location, issues);
                CheckRequiredString(step.type, "step.type", "Step type is missing.", location, issues);
                AddId(step.id, "step", location, seenIds, issues);

                if (!string.IsNullOrWhiteSpace(step.type) && !KnownStepTypes.Contains(step.type))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "step.unknownType",
                        "Unknown step type '" + step.type + "'.",
                        location));
                    continue;
                }

                ValidateCommonAnchorReference(step, anchorIds, location, issues);
                
                ValidateFlowReferences(step, stepIds, location, issues);

                if (step.type == "showObject")
                {
                    ValidateObjectIdParameter(step, objectIds, location, issues);
                }
                else if (step.type == "moveObject")
                {
                    ValidateObjectIdParameter(step, objectIds, location, issues);
                }
                else if (step.type == "rotateJoint")
                {
                    ValidateObjectIdParameter(step, objectIds, location, issues);
                    ValidateJointIndexParameter(step, location, issues);
                    ValidateAngleDegreesParameter(step, location, issues);
                }
                else if (step.type == "showFrame")
                {
                    ValidateObjectIdParameter(step, objectIds, location, issues);
                    ValidateJointIndexParameter(step, location, issues);
                }
                else if (step.type == "image")
                {
                    ValidateAssetIdParameter(step, assetIds, location, issues);
                }
                else if (step.type == "mcq")
                {
                    ValidateMcq(step, location, issues);
                }
            }
        }

        private static void ValidateCommonAnchorReference(
            ModuleStep step,
            HashSet<string> anchorIds,
            string location,
            List<ValidationIssue> issues)
        {
            string anchorId = step.GetString("anchorId", "");
            if (!string.IsNullOrWhiteSpace(anchorId) && !anchorIds.Contains(anchorId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step.anchorId.unknown",
                    "Step references unknown anchor id '" + anchorId + "'.",
                    location));
            }
        }

        private static void ValidateObjectIdParameter(
            ModuleStep step,
            HashSet<string> objectIds,
            string location,
            List<ValidationIssue> issues)
        {
            string objectId = step.GetString("objectId", "");
            if (string.IsNullOrWhiteSpace(objectId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step.objectId.missing",
                    "Step is missing required parameter objectId.",
                    location));
                return;
            }

            if (!objectIds.Contains(objectId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step.objectId.unknown",
                    "Step references unknown object id '" + objectId + "'.",
                    location));
            }
        }

        private static void ValidateAssetIdParameter(
            ModuleStep step,
            HashSet<string> assetIds,
            string location,
            List<ValidationIssue> issues)
        {
            string assetId = step.GetString("assetId", "");
            if (string.IsNullOrWhiteSpace(assetId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step.assetId.missing",
                    "Image step is missing required parameter assetId.",
                    location));
                return;
            }

            if (!assetIds.Contains(assetId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step.assetId.unknown",
                    "Image step references unknown asset id '" + assetId + "'.",
                    location));
            }
        }

        private static void ValidateMcq(ModuleStep step, string location, List<ValidationIssue> issues)
        {
            string question = step.GetString("question", "");
            if (string.IsNullOrWhiteSpace(question))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "mcq.question.missing",
                    "MCQ step is missing question.",
                    location));
            }

            JToken choicesToken = step.GetToken("choices");
            JArray choices = choicesToken as JArray;
            if (choices == null || choices.Count == 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "mcq.choices.empty",
                    "MCQ step must contain at least one choice.",
                    location));
                return;
            }

            int correctIndex = step.GetInt("correctIndex", -1);
            if (correctIndex < 0 || correctIndex >= choices.Count)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "mcq.correctIndex.invalid",
                    "MCQ correctIndex is outside the choices array.",
                    location));
            }
        }

        private static void ValidateJointIndexParameter(
            ModuleStep step,
            string location,
            List<ValidationIssue> issues)
        {
            int jointIndex = step.GetInt("jointIndex", -1);
            if (jointIndex < 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "robotics.jointIndex.invalid",
                    "Robotics step must have a non-negative jointIndex.",
                    location));
            }
        }

        private static void ValidateAngleDegreesParameter(
            ModuleStep step,
            string location,
            List<ValidationIssue> issues)
        {
            JToken token = step.GetToken("angleDegrees");
            if (token == null || token.Type == JTokenType.Null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "robotics.angleDegrees.missing",
                    "rotateJoint step is missing angleDegrees.",
                    location));
            }
        }

        private static void CheckRequiredString(
            string value,
            string code,
            string message,
            List<ValidationIssue> issues)
        {
            CheckRequiredString(value, code, message, "", issues);
        }

        private static void CheckRequiredString(
            string value,
            string code,
            string message,
            string location,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, code, message, location));
            }
        }

        private static void AddId(
            string id,
            string ownerKind,
            string location,
            Dictionary<string, string> seenIds,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (seenIds.ContainsKey(id))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "id.duplicate",
                    "Duplicate id '" + id + "'. First seen as " + seenIds[id] + ".",
                    location));
                return;
            }

            seenIds.Add(id, ownerKind);
        }

        private static HashSet<string> CollectLayoutTargetIds(ModuleDocument document)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

            if (document == null)
            {
                return ids;
            }

            AddIfNotEmpty(ids, document.moduleId);

            if (document.assets != null)
            {
                for (int i = 0; i < document.assets.Count; i++)
                {
                    if (document.assets[i] != null) AddIfNotEmpty(ids, document.assets[i].id);
                }
            }

            if (document.objects != null)
            {
                for (int i = 0; i < document.objects.Count; i++)
                {
                    if (document.objects[i] != null) AddIfNotEmpty(ids, document.objects[i].id);
                }
            }

            if (document.steps != null)
            {
                for (int i = 0; i < document.steps.Count; i++)
                {
                    if (document.steps[i] != null) AddIfNotEmpty(ids, document.steps[i].id);
                }
            }

            return ids;
        }

        private static HashSet<string> CollectStepIds(ModuleDocument document)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

            if (document == null || document.steps == null)
            {
                return ids;
            }

            for (int i = 0; i < document.steps.Count; i++)
            {
                ModuleStep step = document.steps[i];
                if (step != null && !string.IsNullOrWhiteSpace(step.id))
                {
                    ids.Add(step.id);
                }
            }

            return ids;
        }

        private static void AddIfNotEmpty(HashSet<string> ids, string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id);
            }
        }

        private static void ValidateFlowReferences(
            ModuleStep step,
            HashSet<string> stepIds,
            string location,
            List<ValidationIssue> issues)
        {
            ValidateStepReferenceParameter(
                step,
                stepIds,
                "nextStepId",
                "flow.nextStepId.unknown",
                "nextStepId",
                location,
                issues);

            bool hasCorrectBranch = !string.IsNullOrWhiteSpace(step.GetString("onCorrectStepId", ""));
            bool hasWrongBranch = !string.IsNullOrWhiteSpace(step.GetString("onWrongStepId", ""));

            if (step.type == "mcq")
            {
                ValidateStepReferenceParameter(
                    step,
                    stepIds,
                    "onCorrectStepId",
                    "flow.onCorrectStepId.unknown",
                    "onCorrectStepId",
                    location,
                    issues);

                ValidateStepReferenceParameter(
                    step,
                    stepIds,
                    "onWrongStepId",
                    "flow.onWrongStepId.unknown",
                    "onWrongStepId",
                    location,
                    issues);
            }
            else if (hasCorrectBranch || hasWrongBranch)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "flow.mcqBranchOnNonMcq",
                    "onCorrectStepId/onWrongStepId are only used by mcq steps.",
                    location));

                // Still validate the references so typos are visible even before the author fixes the step type.
                ValidateStepReferenceParameter(
                    step,
                    stepIds,
                    "onCorrectStepId",
                    "flow.onCorrectStepId.unknown",
                    "onCorrectStepId",
                    location,
                    issues);

                ValidateStepReferenceParameter(
                    step,
                    stepIds,
                    "onWrongStepId",
                    "flow.onWrongStepId.unknown",
                    "onWrongStepId",
                    location,
                    issues);
            }
        }

        private static void ValidateStepReferenceParameter(
            ModuleStep step,
            HashSet<string> stepIds,
            string parameterKey,
            string issueCode,
            string displayName,
            string location,
            List<ValidationIssue> issues)
        {
            string targetStepId = step.GetString(parameterKey, "");
            if (string.IsNullOrWhiteSpace(targetStepId))
            {
                return;
            }

            if (stepIds == null || !stepIds.Contains(targetStepId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    issueCode,
                    displayName + " references unknown step id '" + targetStepId + "'.",
                    location));
            }
        }
    }
}