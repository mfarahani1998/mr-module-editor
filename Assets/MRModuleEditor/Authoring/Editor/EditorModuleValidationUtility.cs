using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Validation;
using MRModuleEditor.Runtime.Anchors;
using MRModuleEditor.Runtime.SceneBinding;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MRModuleEditor.Authoring.Editor
{
    public static class EditorModuleValidationUtility
    {
        private static readonly Dictionary<string, bool> GroupFoldouts = new Dictionary<string, bool>(StringComparer.Ordinal);

        public static List<ValidationIssue> CollectIssues(ModuleDocument document, string moduleJsonPath)
        {
            return CollectIssues(document, moduleJsonPath, true);
        }

        public static List<ValidationIssue> CollectIssues(
            ModuleDocument document,
            string moduleJsonPath,
            bool includeSceneBindingChecks)
        {
            List<ValidationIssue> issues = ModuleValidator.Validate(document);
            AddAssetFileIssues(document, moduleJsonPath, issues);

            if (includeSceneBindingChecks)
            {
                AddSceneBindingIssues(document, issues);
            }

            return issues;
        }

        public static bool HasError(List<ValidationIssue> issues)
        {
            return ModuleValidator.HasError(issues);
        }

        public static string FormatIssueList(List<ValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                return "Validation passed.";
            }

            return string.Join("\n", issues.Select(issue => issue.ToString()).ToArray());
        }

        public static void DrawGroupedIssues(List<ValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Validation passed.", MessageType.Info);
                return;
            }

            ValidationSeverity worst = WorstSeverity(issues);
            MessageType summaryType = ToMessageType(worst);
            int errorCount = CountSeverity(issues, ValidationSeverity.Error);
            int warningCount = CountSeverity(issues, ValidationSeverity.Warning);
            int infoCount = CountSeverity(issues, ValidationSeverity.Info);

            EditorGUILayout.HelpBox(
                "Validation found " + errorCount + " error(s), " + warningCount + " warning(s), and " + infoCount + " info item(s). " +
                "Open the groups below to fix them incrementally.",
                summaryType);

            Dictionary<string, List<ValidationIssue>> grouped = GroupIssues(issues);
            string[] order =
            {
                "Module",
                "Assets",
                "Objects",
                "Anchors",
                "Layouts",
                "Steps",
                "Flow",
                "Runtime Scene",
                "Other"
            };

            for (int i = 0; i < order.Length; i++)
            {
                string groupName = order[i];
                List<ValidationIssue> groupIssues;
                if (!grouped.TryGetValue(groupName, out groupIssues) || groupIssues.Count == 0)
                {
                    continue;
                }

                DrawIssueGroup(groupName, groupIssues);
            }
        }

        public static bool TryGetSceneBindingStatus(ModuleObject moduleObject, out string message, out MessageType messageType)
        {
            message = "";
            messageType = MessageType.Info;

            if (moduleObject == null)
            {
                message = "Object entry is null.";
                messageType = MessageType.Error;
                return false;
            }

            if (string.IsNullOrWhiteSpace(moduleObject.bindingKey))
            {
                message = "No binding key has been assigned yet.";
                messageType = MessageType.Warning;
                return false;
            }

            Dictionary<string, int> bindingCounts = CountSceneBindingKeys();
            int count;
            if (!bindingCounts.TryGetValue(moduleObject.bindingKey, out count) || count <= 0)
            {
                message =
                    "No BindableObject with binding key '" + moduleObject.bindingKey + "' was found in the active scene. " +
                    "This is okay while authoring JSON, but Preview will need a matching scene object.";
                messageType = MessageType.Warning;
                return false;
            }

            if (count > 1)
            {
                message =
                    "Found " + count + " BindableObjects with binding key '" + moduleObject.bindingKey + "'. " +
                    "Runtime uses the first one it registers, so make this key unique.";
                messageType = MessageType.Warning;
                return false;
            }

            message = "Active scene has a matching BindableObject for binding key '" + moduleObject.bindingKey + "'.";
            messageType = MessageType.Info;
            return true;
        }

        public static bool TryGetAnchorAuthoringStatus(
            ModuleDocument document,
            AnchorDefinition anchor,
            out string message,
            out MessageType messageType)
        {
            message = "";
            messageType = MessageType.Info;

            if (anchor == null)
            {
                message = "Anchor entry is null.";
                messageType = MessageType.Error;
                return false;
            }

            if (anchor.type == "head")
            {
                Camera camera = Camera.main;
                if (camera == null)
                {
                    message = "No Main Camera is currently tagged in the active scene. Head anchors will resolve once the runtime preview scene provides one.";
                    messageType = MessageType.Warning;
                    return false;
                }

                message = "Head anchor can resolve from active scene Main Camera '" + camera.name + "'.";
                messageType = MessageType.Info;
                return true;
            }

            if (anchor.type == "world")
            {
                AnchorResolver resolver = Object.FindFirstObjectByType<AnchorResolver>(FindObjectsInactive.Include);
                if (resolver == null)
                {
                    message = "No AnchorResolver was found in the active scene. The runtime preview scene must contain one for world anchors and recentering.";
                    messageType = MessageType.Warning;
                    return false;
                }

                message = "World anchor can resolve through active scene AnchorResolver '" + resolver.name + "'. Use the runtime recenter panel to calibrate it during preview.";
                messageType = MessageType.Info;
                return true;
            }

            if (anchor.type == "object")
            {
                if (string.IsNullOrWhiteSpace(anchor.targetObjectId))
                {
                    message = "Object anchor needs a target object id.";
                    messageType = MessageType.Warning;
                    return false;
                }

                ModuleObject target = FindModuleObject(document, anchor.targetObjectId);
                if (target == null)
                {
                    message = "Object anchor targets unknown module object id '" + anchor.targetObjectId + "'.";
                    messageType = MessageType.Error;
                    return false;
                }

                bool ok = TryGetSceneBindingStatus(target, out message, out messageType);
                if (ok)
                {
                    message = "Object anchor target is ready. " + message;
                }
                else
                {
                    message = "Object anchor target is not ready yet. " + message;
                }

                return ok;
            }

            message = "Unknown anchor type '" + anchor.type + "'. Expected head, world, or object.";
            messageType = MessageType.Error;
            return false;
        }

        private static void AddAssetFileIssues(ModuleDocument document, string moduleJsonPath, List<ValidationIssue> issues)
        {
            if (document == null || document.assets == null || document.assets.Count == 0)
            {
                return;
            }

            string moduleFolder = "";
            bool canCheckFiles = TryGetModuleFolderForValidation(moduleJsonPath, out moduleFolder);
            if (!canCheckFiles)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "asset.modulePath.unsaved",
                    "Save the module as module.json before relying on asset file-existence checks.",
                    "assets"));
                return;
            }

            for (int i = 0; i < document.assets.Count; i++)
            {
                ModuleAsset asset = document.assets[i];
                string location = "assets[" + i + "]";
                if (asset == null || string.IsNullOrWhiteSpace(asset.path))
                {
                    continue;
                }

                if (LooksLikeUrl(asset.path))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Info,
                        "asset.path.url",
                        "Asset path looks like a URL. The editor cannot verify or copy it during local module export.",
                        location));
                    continue;
                }

                if (ContainsParentTraversal(asset.path))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "asset.path.traversal",
                        "Asset path should stay inside the module folder. Remove '..' path segments.",
                        location));
                    continue;
                }

                if (Path.IsPathRooted(asset.path))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "asset.path.absolute",
                        "Asset path is absolute. Prefer a path relative to the module folder so export/import stays portable.",
                        location));
                }

                string absolutePath = Path.IsPathRooted(asset.path)
                    ? asset.path
                    : Path.Combine(moduleFolder, asset.path);
                absolutePath = NormalizeFullPath(absolutePath);

                if (!File.Exists(absolutePath))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "asset.fileMissing",
                        "Asset file does not exist on disk: " + asset.path,
                        location));
                }

                if (EditorAssetImportUtility.IsKnownAssetType(asset.type)
                    && !EditorAssetImportUtility.ExtensionMatchesType(asset.path, asset.type))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "asset.extension.mismatch",
                        "Asset type '" + asset.type + "' does not match file extension '" + Path.GetExtension(asset.path) + "'.",
                        location));
                }
            }
        }

        private static void AddSceneBindingIssues(ModuleDocument document, List<ValidationIssue> issues)
        {
            if (document == null || document.objects == null)
            {
                return;
            }

            Dictionary<string, int> bindingCounts = CountSceneBindingKeys();
            for (int i = 0; i < document.objects.Count; i++)
            {
                ModuleObject moduleObject = document.objects[i];
                string location = "objects[" + i + "]";
                if (moduleObject == null || string.IsNullOrWhiteSpace(moduleObject.bindingKey))
                {
                    continue;
                }

                int count;
                if (!bindingCounts.TryGetValue(moduleObject.bindingKey, out count) || count <= 0)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "sceneBinding.missing",
                        "Active scene has no BindableObject with binding key '" + moduleObject.bindingKey + "'.",
                        location));
                    continue;
                }

                if (count > 1)
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "sceneBinding.duplicate",
                        "Active scene has " + count + " BindableObjects with binding key '" + moduleObject.bindingKey + "'. Use unique binding keys.",
                        location));
                }
            }
        }

        private static bool TryGetModuleFolderForValidation(string moduleJsonPath, out string moduleFolder)
        {
            moduleFolder = "";
            if (string.IsNullOrWhiteSpace(moduleJsonPath))
            {
                return false;
            }

            string fullPath;
            try
            {
                fullPath = NormalizeFullPath(moduleJsonPath);
            }
            catch
            {
                return false;
            }

            if (!string.Equals(Path.GetFileName(fullPath), "module.json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            moduleFolder = Path.GetDirectoryName(fullPath);
            return !string.IsNullOrWhiteSpace(moduleFolder);
        }

        private static ModuleObject FindModuleObject(ModuleDocument document, string objectId)
        {
            if (document == null || document.objects == null || string.IsNullOrWhiteSpace(objectId))
            {
                return null;
            }

            for (int i = 0; i < document.objects.Count; i++)
            {
                ModuleObject moduleObject = document.objects[i];
                if (moduleObject != null && moduleObject.id == objectId)
                {
                    return moduleObject;
                }
            }

            return null;
        }

        private static Dictionary<string, int> CountSceneBindingKeys()
        {
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
            BindableObject[] bindables = Object.FindObjectsByType<BindableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < bindables.Length; i++)
            {
                BindableObject bindable = bindables[i];
                if (bindable == null || string.IsNullOrWhiteSpace(bindable.BindingKey))
                {
                    continue;
                }

                if (!result.ContainsKey(bindable.BindingKey))
                {
                    result.Add(bindable.BindingKey, 0);
                }

                result[bindable.BindingKey]++;
            }

            return result;
        }

        private static Dictionary<string, List<ValidationIssue>> GroupIssues(List<ValidationIssue> issues)
        {
            Dictionary<string, List<ValidationIssue>> grouped = new Dictionary<string, List<ValidationIssue>>(StringComparer.Ordinal);
            for (int i = 0; i < issues.Count; i++)
            {
                ValidationIssue issue = issues[i];
                string groupName = GetGroupName(issue);
                if (!grouped.ContainsKey(groupName))
                {
                    grouped.Add(groupName, new List<ValidationIssue>());
                }

                grouped[groupName].Add(issue);
            }

            return grouped;
        }

        private static void DrawIssueGroup(string groupName, List<ValidationIssue> issues)
        {
            bool expanded;
            if (!GroupFoldouts.TryGetValue(groupName, out expanded))
            {
                expanded = true;
            }

            ValidationSeverity worst = WorstSeverity(issues);
            string label = groupName + " — " + issues.Count + " issue(s), worst: " + worst;
            expanded = EditorGUILayout.Foldout(expanded, label, true);
            GroupFoldouts[groupName] = expanded;

            if (!expanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()),
                ToMessageType(worst));
            EditorGUI.indentLevel--;
        }

        private static string GetGroupName(ValidationIssue issue)
        {
            string location = issue == null ? "" : issue.location ?? "";
            string code = issue == null ? "" : issue.code ?? "";

            if (location.StartsWith("assets", StringComparison.Ordinal) || code.StartsWith("asset.", StringComparison.Ordinal)) return "Assets";
            if (code.StartsWith("sceneBinding.", StringComparison.Ordinal)) return "Runtime Scene";
            if (location.StartsWith("objects", StringComparison.Ordinal) || code.StartsWith("object.", StringComparison.Ordinal)) return "Objects";
            if (location.StartsWith("anchors", StringComparison.Ordinal) || code.StartsWith("anchor.", StringComparison.Ordinal)) return "Anchors";
            if (location.StartsWith("layouts", StringComparison.Ordinal) || code.StartsWith("layout.", StringComparison.Ordinal)) return "Layouts";
            if (code.StartsWith("flow.", StringComparison.Ordinal)) return "Flow";
            if (location.StartsWith("steps", StringComparison.Ordinal) || code.StartsWith("step.", StringComparison.Ordinal) || code.StartsWith("mcq.", StringComparison.Ordinal) || code.StartsWith("audio.", StringComparison.Ordinal)) return "Steps";
            if (location.StartsWith("document", StringComparison.Ordinal) || code.StartsWith("document.", StringComparison.Ordinal)) return "Module";
            return "Other";
        }

        private static ValidationSeverity WorstSeverity(List<ValidationIssue> issues)
        {
            ValidationSeverity worst = ValidationSeverity.Info;
            if (issues == null)
            {
                return worst;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].severity == ValidationSeverity.Error)
                {
                    return ValidationSeverity.Error;
                }

                if (issues[i].severity == ValidationSeverity.Warning)
                {
                    worst = ValidationSeverity.Warning;
                }
            }

            return worst;
        }

        private static int CountSeverity(List<ValidationIssue> issues, ValidationSeverity severity)
        {
            if (issues == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        private static MessageType ToMessageType(ValidationSeverity severity)
        {
            if (severity == ValidationSeverity.Error) return MessageType.Error;
            if (severity == ValidationSeverity.Warning) return MessageType.Warning;
            return MessageType.Info;
        }

        private static bool LooksLikeUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalized = path.Replace("\\", "/");
            return normalized.Contains("://") || normalized.StartsWith("jar:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsParentTraversal(string path)
        {
            string normalized = (path ?? "").Replace("\\", "/");
            string[] parts = normalized.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "..")
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            return Path.GetFullPath(path).Replace("\\", "/").TrimEnd('/');
        }
    }
}
