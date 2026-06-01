using System;
using System.Collections.Generic;
using System.Globalization;
using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using Newtonsoft.Json.Linq;

namespace MRModuleEditor.Core.Validation
{
    public static class ModuleValidator
    {
        private static readonly HashSet<string> KnownAnchorTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "head",
            "world",
            "object"
        };

        public static List<ValidationIssue> Validate(ModuleDocument document)
        {
            return Validate(document, StepCatalog.Global);
        }

        public static List<ValidationIssue> Validate(ModuleDocument document, StepCatalog stepCatalog)
        {
            Dictionary<string, string> assetTypesById = new Dictionary<string, string>(StringComparer.Ordinal);
            List<ValidationIssue> issues = new List<ValidationIssue>();

            if (document == null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "document.null",
                    "ModuleDocument is null."));
                return issues;
            }

            StepCatalog catalog = stepCatalog ?? StepCatalog.Global;
            if (catalog == null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "stepCatalog.missing",
                    "Step catalog is missing."));
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

            ValidateAssets(document, assetIds, assetTypesById, seenIds, issues);
            ValidateObjects(document, objectIds, seenIds, issues);
            ValidateAnchors(document, objectIds, anchorIds, seenIds, issues);
            ValidateLayouts(document, anchorIds, layoutTargetIds, seenIds, issues);
            LayoutReadabilityValidator.AddIssues(document, issues);
            ValidateSteps(document, catalog, objectIds, assetIds, assetTypesById, anchorIds, stepIds, seenIds, issues);

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
            Dictionary<string, string> assetTypesById,
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
                    if (assetTypesById != null && !assetTypesById.ContainsKey(asset.id))
                    {
                        assetTypesById.Add(asset.id, asset.type ?? "");
                    }
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
            StepCatalog catalog,
            HashSet<string> objectIds,
            HashSet<string> assetIds,
            Dictionary<string, string> assetTypesById,
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

            StepValidationContext context = new StepValidationContext(
                document,
                objectIds,
                assetIds,
                assetTypesById,
                anchorIds,
                stepIds);

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

                StepTypeDefinition definition = null;
                if (!string.IsNullOrWhiteSpace(step.type) && !catalog.TryGet(step.type, out definition))
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

                if (definition != null)
                {
                    ValidateCatalogParameters(step, definition, context, location, issues);
                }
            }
        }

        private static void ValidateCatalogParameters(
            ModuleStep step,
            StepTypeDefinition definition,
            StepValidationContext context,
            string location,
            List<ValidationIssue> issues)
        {
            StepParameterDefinition[] parameters = definition.Parameters ?? new StepParameterDefinition[0];
            for (int i = 0; i < parameters.Length; i++)
            {
                StepParameterDefinition parameter = parameters[i];
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.Key))
                {
                    continue;
                }

                if (!IsParameterActive(step, parameter))
                {
                    continue;
                }

                JToken token = step.GetToken(parameter.Key);
                if (IsMissingToken(token))
                {
                    if (parameter.Required)
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            "step." + parameter.Key + ".missing",
                            "Step is missing required parameter " + parameter.Key + ".",
                            location));
                    }

                    continue;
                }

                ValidateParameterKind(step, parameter, token, context, location, issues);
            }

            if (definition.CustomValidator != null)
            {
                definition.CustomValidator(step, context, location, issues);
            }
        }

        private static bool IsParameterActive(ModuleStep step, StepParameterDefinition parameter)
        {
            if (!parameter.HasVisibilityCondition)
            {
                return true;
            }

            return step.GetBool(parameter.VisibleWhenParameterKey, !parameter.VisibleWhenBoolValue) == parameter.VisibleWhenBoolValue;
        }

        private static bool IsMissingToken(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return true;
            }

            if (token.Type == JTokenType.String && string.IsNullOrWhiteSpace(token.Value<string>()))
            {
                return true;
            }

            return false;
        }

        private static void ValidateParameterKind(
            ModuleStep step,
            StepParameterDefinition parameter,
            JToken token,
            StepValidationContext context,
            string location,
            List<ValidationIssue> issues)
        {
            switch (parameter.Kind)
            {
                case StepParameterKind.ObjectId:
                    ValidateObjectReference(step.GetString(parameter.Key, ""), context.ObjectIds, parameter.Key, location, issues);
                    break;
                case StepParameterKind.AssetId:
                    ValidateAssetReference(step.GetString(parameter.Key, ""), context.AssetIds, parameter.Key, location, issues);
                    ValidateAssetType(step.GetString(parameter.Key, ""), context.AssetTypesById, parameter.ExpectedAssetType, location, issues);
                    break;
                case StepParameterKind.AnchorId:
                    if (parameter.Key != "anchorId")
                    {
                        ValidateAnchorReference(step.GetString(parameter.Key, ""), context.AnchorIds, parameter.Key, location, issues);
                    }
                    break;
                case StepParameterKind.StepId:
                    ValidateStepReference(step.GetString(parameter.Key, ""), context.StepIds, parameter.Key, "flow." + parameter.Key + ".unknown", location, issues);
                    break;
                case StepParameterKind.Int:
                    ValidateInt(parameter.Key, token, location, issues);
                    break;
                case StepParameterKind.Float:
                    ValidateFloat(parameter.Key, token, location, issues);
                    break;
                case StepParameterKind.Bool:
                    ValidateBool(parameter.Key, token, location, issues);
                    break;
                case StepParameterKind.Vector3:
                    ValidateVector3(parameter.Key, token, location, issues);
                    break;
                case StepParameterKind.StringArray:
                    if (!(token is JArray))
                    {
                        issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            "step." + parameter.Key + ".invalidStringArray",
                            "Parameter " + parameter.Key + " must be a JSON array of strings.",
                            location));
                    }
                    break;
            }
        }

        private static void ValidateCommonAnchorReference(
            ModuleStep step,
            HashSet<string> anchorIds,
            string location,
            List<ValidationIssue> issues)
        {
            string anchorId = step.GetString("anchorId", "");
            ValidateAnchorReference(anchorId, anchorIds, "anchorId", location, issues);
        }

        private static void ValidateObjectReference(
            string objectId,
            HashSet<string> objectIds,
            string parameterKey,
            string location,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                return;
            }

            if (!objectIds.Contains(objectId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step." + parameterKey + ".unknown",
                    "Step references unknown object id '" + objectId + "'.",
                    location));
            }
        }

        private static void ValidateAssetReference(
            string assetId,
            HashSet<string> assetIds,
            string parameterKey,
            string location,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return;
            }

            if (!assetIds.Contains(assetId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step." + parameterKey + ".unknown",
                    "Step references unknown asset id '" + assetId + "'.",
                    location));
            }
        }

        private static void ValidateAnchorReference(
            string anchorId,
            HashSet<string> anchorIds,
            string parameterKey,
            string location,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                return;
            }

            if (!anchorIds.Contains(anchorId))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    parameterKey == "anchorId" ? "step.anchorId.unknown" : "step." + parameterKey + ".unknown",
                    "Step references unknown anchor id '" + anchorId + "'.",
                    location));
            }
        }

        private static void ValidateStepReference(
            string targetStepId,
            HashSet<string> stepIds,
            string displayName,
            string issueCode,
            string location,
            List<ValidationIssue> issues)
        {
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

        private static void ValidateInt(string parameterKey, JToken token, string location, List<ValidationIssue> issues)
        {
            int parsed;
            if (!int.TryParse(token.ToString(), out parsed))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step." + parameterKey + ".invalidInt",
                    "Parameter " + parameterKey + " must be an integer.",
                    location));
            }
        }

        private static void ValidateFloat(string parameterKey, JToken token, string location, List<ValidationIssue> issues)
        {
            float parsed;
            if (token == null || token.Type == JTokenType.Null || !float.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step." + parameterKey + ".invalidFloat",
                    "Parameter " + parameterKey + " must be a number.",
                    location));
            }
        }

        private static void ValidateBool(string parameterKey, JToken token, string location, List<ValidationIssue> issues)
        {
            bool parsed;
            if (!bool.TryParse(token.ToString(), out parsed))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step." + parameterKey + ".invalidBool",
                    "Parameter " + parameterKey + " must be true or false.",
                    location));
            }
        }

        private static void ValidateVector3(string parameterKey, JToken token, string location, List<ValidationIssue> issues)
        {
            if (token == null || token.Type != JTokenType.Object)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step." + parameterKey + ".invalidVector3",
                    "Parameter " + parameterKey + " must be an object with x, y, and z fields.",
                    location));
                return;
            }

            ValidateFloat(parameterKey + ".x", token["x"], location, issues);
            ValidateFloat(parameterKey + ".y", token["y"], location, issues);
            ValidateFloat(parameterKey + ".z", token["z"], location, issues);
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
            ValidateStepReference(
                step.GetString("nextStepId", ""),
                stepIds,
                "nextStepId",
                "flow.nextStepId.unknown",
                location,
                issues);

            bool hasCorrectBranch = !string.IsNullOrWhiteSpace(step.GetString("onCorrectStepId", ""));
            bool hasWrongBranch = !string.IsNullOrWhiteSpace(step.GetString("onWrongStepId", ""));

            if (step.type == "mcq")
            {
                ValidateStepReference(
                    step.GetString("onCorrectStepId", ""),
                    stepIds,
                    "onCorrectStepId",
                    "flow.onCorrectStepId.unknown",
                    location,
                    issues);

                ValidateStepReference(
                    step.GetString("onWrongStepId", ""),
                    stepIds,
                    "onWrongStepId",
                    "flow.onWrongStepId.unknown",
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

                ValidateStepReference(
                    step.GetString("onCorrectStepId", ""),
                    stepIds,
                    "onCorrectStepId",
                    "flow.onCorrectStepId.unknown",
                    location,
                    issues);

                ValidateStepReference(
                    step.GetString("onWrongStepId", ""),
                    stepIds,
                    "onWrongStepId",
                    "flow.onWrongStepId.unknown",
                    location,
                    issues);
            }
        }

        private static void ValidateAssetType(
            string assetId,
            Dictionary<string, string> assetTypesById,
            string expectedType,
            string location,
            List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(assetId) || string.IsNullOrWhiteSpace(expectedType) || assetTypesById == null)
            {
                return;
            }

            string actualType;
            if (!assetTypesById.TryGetValue(assetId, out actualType))
            {
                return;
            }

            if (actualType != expectedType)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "step.assetId.wrongType",
                    "Step expects an asset of type '" + expectedType + "' but asset '" + assetId + "' has type '" + actualType + "'.",
                    location));
            }
        }
    }
}
