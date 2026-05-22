using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class EditorIdDropdowns
    {
        private struct IdOption
        {
            public readonly string id;
            public readonly string display;

            public IdOption(string id, string display)
            {
                this.id = id ?? "";
                this.display = string.IsNullOrWhiteSpace(display) ? this.id : display;
            }
        }

        public static bool DrawAssetIdDropdown(
            ModuleDocument document,
            ModuleStep step,
            string parameterKey,
            string label,
            string typeFilter = "")
        {
            if (step == null)
            {
                return false;
            }

            string current = step.GetString(parameterKey, "");
            bool changed = DrawAssetIdDropdown(document, ref current, label, typeFilter);
            if (changed)
            {
                SetStepString(step, parameterKey, current);
            }

            return changed;
        }

        public static bool DrawObjectIdDropdown(
            ModuleDocument document,
            ModuleStep step,
            string parameterKey,
            string label,
            bool allowNone = false)
        {
            if (step == null)
            {
                return false;
            }

            string current = step.GetString(parameterKey, "");
            bool changed = DrawObjectIdDropdown(document, ref current, label, allowNone);
            if (changed)
            {
                SetStepString(step, parameterKey, current);
            }

            return changed;
        }

        public static bool DrawAnchorIdDropdown(
            ModuleDocument document,
            ModuleStep step,
            string parameterKey,
            string label,
            bool allowNone = false)
        {
            if (step == null)
            {
                return false;
            }

            string current = step.GetString(parameterKey, "");
            bool changed = DrawAnchorIdDropdown(document, ref current, label, allowNone);
            if (changed)
            {
                SetStepString(step, parameterKey, current);
            }

            return changed;
        }

        public static bool DrawAssetIdDropdown(
            ModuleDocument document,
            ref string assetId,
            string label,
            string typeFilter = "")
        {
            List<IdOption> options = new List<IdOption>();

            if (document != null && document.assets != null)
            {
                for (int i = 0; i < document.assets.Count; i++)
                {
                    ModuleAsset asset = document.assets[i];
                    if (asset == null || string.IsNullOrWhiteSpace(asset.id))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(typeFilter) && asset.type != typeFilter)
                    {
                        continue;
                    }

                    options.Add(new IdOption(asset.id, FormatAsset(asset)));
                }
            }

            return DrawIdPopupOrText(label, ref assetId, options);
        }

        public static bool DrawObjectIdDropdown(
            ModuleDocument document,
            ref string objectId,
            string label,
            bool allowNone = false)
        {
            List<IdOption> options = new List<IdOption>();
            if (allowNone)
            {
                options.Add(new IdOption("", "<None>"));
            }

            if (document != null && document.objects != null)
            {
                for (int i = 0; i < document.objects.Count; i++)
                {
                    ModuleObject moduleObject = document.objects[i];
                    if (moduleObject == null || string.IsNullOrWhiteSpace(moduleObject.id))
                    {
                        continue;
                    }

                    options.Add(new IdOption(moduleObject.id, FormatObject(moduleObject)));
                }
            }

            return DrawIdPopupOrText(label, ref objectId, options);
        }

        public static bool DrawAnchorIdDropdown(
            ModuleDocument document,
            ref string anchorId,
            string label,
            bool allowNone = false)
        {
            List<IdOption> options = new List<IdOption>();
            if (allowNone)
            {
                options.Add(new IdOption("", "<None>"));
            }

            if (document != null && document.anchors != null)
            {
                for (int i = 0; i < document.anchors.Count; i++)
                {
                    AnchorDefinition anchor = document.anchors[i];
                    if (anchor == null || string.IsNullOrWhiteSpace(anchor.id))
                    {
                        continue;
                    }

                    options.Add(new IdOption(anchor.id, FormatAnchor(document, anchor)));
                }
            }

            return DrawIdPopupOrText(label, ref anchorId, options);
        }

        private static bool DrawIdPopupOrText(string label, ref string currentId, List<IdOption> options)
        {
            currentId = currentId ?? "";

            if (options == null || options.Count == 0)
            {
                string nextText = EditorGUILayout.TextField(label, currentId);
                if (nextText == currentId)
                {
                    return false;
                }

                currentId = nextText;
                return true;
            }

            bool currentExists = false;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].id == currentId)
                {
                    currentExists = true;
                    break;
                }
            }

            if (!currentExists)
            {
                if (string.IsNullOrWhiteSpace(currentId))
                {
                    options.Insert(0, new IdOption("", "<Empty>"));
                }
                else
                {
                    options.Insert(0, new IdOption(currentId, "<Missing>  " + currentId));
                }
            }

            int currentIndex = 0;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].id == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            string[] displayLabels = new string[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                displayLabels[i] = options[i].display;
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, displayLabels);
            nextIndex = Mathf.Clamp(nextIndex, 0, options.Count - 1);
            string nextId = options[nextIndex].id;

            if (nextId == currentId)
            {
                return false;
            }

            currentId = nextId;
            return true;
        }

        private static void SetStepString(ModuleStep step, string parameterKey, string value)
        {
            if (step.parameters == null)
            {
                step.parameters = new Dictionary<string, JToken>();
            }

            step.parameters[parameterKey] = JToken.FromObject(value ?? "");
        }

        private static string FormatAsset(ModuleAsset asset)
        {
            string main = string.IsNullOrWhiteSpace(asset.label) ? asset.id : asset.label;
            string type = string.IsNullOrWhiteSpace(asset.type) ? "" : " [" + asset.type + "]";
            return main + type + "  (" + asset.id + ")";
        }

        private static string FormatObject(ModuleObject moduleObject)
        {
            string main = string.IsNullOrWhiteSpace(moduleObject.label) ? moduleObject.id : moduleObject.label;
            string binding = string.IsNullOrWhiteSpace(moduleObject.bindingKey) ? "" : " -> " + moduleObject.bindingKey;
            return main + binding + "  (" + moduleObject.id + ")";
        }

        private static string FormatAnchor(ModuleDocument document, AnchorDefinition anchor)
        {
            string type = string.IsNullOrWhiteSpace(anchor.type) ? "unknown" : anchor.type;
            string suffix = "";

            if (anchor.type == "object" && !string.IsNullOrWhiteSpace(anchor.targetObjectId))
            {
                ModuleObject target = EditorModuleDataUtility.FindObject(document, anchor.targetObjectId);
                string targetLabel = target == null || string.IsNullOrWhiteSpace(target.label)
                    ? anchor.targetObjectId
                    : target.label;
                suffix = " -> " + targetLabel;
            }

            return type + suffix + "  (" + anchor.id + ")";
        }
    }
}