using System;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Utilities;

namespace MRModuleEditor.Authoring.Editor
{
    public static class EditorModuleDataUtility
    {
        public static void EnsureLists(ModuleDocument document)
        {
            if (document == null)
            {
                return;
            }

            if (document.assets == null) document.assets = new List<ModuleAsset>();
            if (document.objects == null) document.objects = new List<ModuleObject>();
            if (document.anchors == null) document.anchors = new List<AnchorDefinition>();
            if (document.layouts == null) document.layouts = new List<LayoutDefinition>();
            if (document.steps == null) document.steps = new List<ModuleStep>();
        }

        public static string MakeUniqueId(ModuleDocument document, string desiredId)
        {
            string baseId = IdGenerator.NormalizePrefix(desiredId);
            HashSet<string> existing = CollectAllIds(document);

            if (!existing.Contains(baseId))
            {
                return baseId;
            }

            int suffix = 2;
            while (existing.Contains(baseId + "." + suffix))
            {
                suffix++;
            }

            return baseId + "." + suffix;
        }

        public static HashSet<string> CollectAllIds(ModuleDocument document)
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

            if (document.anchors != null)
            {
                for (int i = 0; i < document.anchors.Count; i++)
                {
                    if (document.anchors[i] != null) AddIfNotEmpty(ids, document.anchors[i].id);
                }
            }

            if (document.layouts != null)
            {
                for (int i = 0; i < document.layouts.Count; i++)
                {
                    if (document.layouts[i] != null) AddIfNotEmpty(ids, document.layouts[i].id);
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

        public static ModuleObject FindObject(ModuleDocument document, string objectId)
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

        public static AnchorDefinition FindAnchor(ModuleDocument document, string anchorId)
        {
            if (document == null || document.anchors == null || string.IsNullOrWhiteSpace(anchorId))
            {
                return null;
            }

            for (int i = 0; i < document.anchors.Count; i++)
            {
                AnchorDefinition anchor = document.anchors[i];
                if (anchor != null && anchor.id == anchorId)
                {
                    return anchor;
                }
            }

            return null;
        }

        public static string FirstObjectId(ModuleDocument document)
        {
            if (document == null || document.objects == null)
            {
                return "";
            }

            for (int i = 0; i < document.objects.Count; i++)
            {
                if (document.objects[i] != null && !string.IsNullOrWhiteSpace(document.objects[i].id))
                {
                    return document.objects[i].id;
                }
            }

            return "";
        }

        public static string FirstAnchorId(ModuleDocument document, string preferredType = "")
        {
            if (document == null || document.anchors == null)
            {
                return "";
            }

            for (int i = 0; i < document.anchors.Count; i++)
            {
                AnchorDefinition anchor = document.anchors[i];
                if (anchor == null || string.IsNullOrWhiteSpace(anchor.id))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(preferredType) || anchor.type == preferredType)
                {
                    return anchor.id;
                }
            }

            return "";
        }

        private static void AddIfNotEmpty(HashSet<string> ids, string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id);
            }
        }
    }
}