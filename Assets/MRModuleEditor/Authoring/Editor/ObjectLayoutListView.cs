using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Utilities;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class ObjectLayoutListView
    {
        public static bool Draw(ModuleDocument document)
        {
            if (document == null)
            {
                return false;
            }

            EditorModuleDataUtility.EnsureLists(document);
            bool changed = false;

            EditorGUILayout.LabelField("Object Layouts", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Object layouts place bound scene objects relative to a module anchor when the module loads. " +
                "This edits layout entries whose targetId is an object id, for example object.equipment_demo.",
                MessageType.Info);

            if (document.objects.Count == 0)
            {
                EditorGUILayout.HelpBox("Add at least one module object before creating object layouts.", MessageType.Info);
                return false;
            }

            for (int i = 0; i < document.objects.Count; i++)
            {
                ModuleObject moduleObject = document.objects[i];
                if (moduleObject == null || string.IsNullOrWhiteSpace(moduleObject.id))
                {
                    continue;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(ObjectTitle(moduleObject), EditorStyles.miniBoldLabel);

                int layoutCount;
                LayoutDefinition layout = FindFirstLayoutForTarget(document, moduleObject.id, out layoutCount);
                if (layout == null)
                {
                    EditorGUILayout.HelpBox("No object layout targets this object yet.", MessageType.None);
                    if (GUILayout.Button("Create Layout For " + ObjectTitle(moduleObject)))
                    {
                        document.layouts.Add(CreateDefaultLayout(document, moduleObject));
                        changed = true;
                    }

                    EditorGUILayout.EndVertical();
                    continue;
                }

                if (layoutCount > 1)
                {
                    EditorGUILayout.HelpBox(
                        "More than one layout targets this object. The runtime currently applies them in list order; keep one object layout per object to avoid surprises.",
                        MessageType.Warning);
                }

                EditorGUI.BeginChangeCheck();
                layout.id = EditorGUILayout.TextField("Layout ID", layout.id);
                if (EditorGUI.EndChangeCheck()) changed = true;

                GUI.enabled = false;
                EditorGUILayout.TextField("Target Object", moduleObject.id);
                GUI.enabled = true;

                if (layout.targetId != moduleObject.id)
                {
                    EditorGUILayout.HelpBox("Layout targetId does not match this object.", MessageType.Warning);
                    if (GUILayout.Button("Repair Target ID"))
                    {
                        layout.targetId = moduleObject.id;
                        changed = true;
                    }
                }

                changed |= EditorIdDropdowns.DrawAnchorIdDropdown(document, ref layout.anchorId, "Anchor");
                changed |= EditorLayoutPresetUtility.DrawPresetButtons(document, layout, LayoutPresetCatalog.ObjectTargetKind);
                changed |= DrawVector3Data("Position", ref layout.position, Vector3.zero, clampPositive: false);
                changed |= DrawVector3Data("Rotation Euler", ref layout.rotationEuler, Vector3.zero, clampPositive: false);
                changed |= DrawVector3Data("Scale", ref layout.scale, Vector3.one, clampPositive: true);

                EditorGUILayout.HelpBox(
                    "For object placement, prefer world anchors and start from one of the object demo presets. " +
                    "Avoid placing an object relative to an anchor that targets the same object unless you are intentionally testing self-relative transforms.",
                    MessageType.None);

                if (GUILayout.Button("Delete Object Layout"))
                {
                    document.layouts.Remove(layout);
                    changed = true;
                }

                EditorGUILayout.EndVertical();
            }

            return changed;
        }

        private static LayoutDefinition FindFirstLayoutForTarget(ModuleDocument document, string targetId, out int count)
        {
            count = 0;
            LayoutDefinition first = null;

            if (document == null || document.layouts == null || string.IsNullOrWhiteSpace(targetId))
            {
                return null;
            }

            for (int i = 0; i < document.layouts.Count; i++)
            {
                LayoutDefinition layout = document.layouts[i];
                if (layout == null || layout.targetId != targetId)
                {
                    continue;
                }

                count++;
                if (first == null)
                {
                    first = layout;
                }
            }

            return first;
        }

        private static LayoutDefinition CreateDefaultLayout(ModuleDocument document, ModuleObject moduleObject)
        {
            string anchorId = EditorModuleDataUtility.FirstAnchorId(document, "world");
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = EditorModuleDataUtility.FirstAnchorId(document);
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = "anchor.world.table";
            }

            string cleanObjectId = IdGenerator.NormalizePrefix(moduleObject.id);
            string layoutId = EditorModuleDataUtility.MakeUniqueId(
                document,
                "layout." + cleanObjectId + ".default");

            return new LayoutDefinition
            {
                id = layoutId,
                targetId = moduleObject.id,
                anchorId = anchorId,
                position = new Vector3Data(0f, 0f, 1.5f),
                rotationEuler = new Vector3Data(0f, 0f, 0f),
                scale = new Vector3Data(1f, 1f, 1f)
            };
        }

        private static bool DrawVector3Data(string label, ref Vector3Data data, Vector3 fallback, bool clampPositive)
        {
            Vector3 current = ReadVector3(data, fallback);
            Vector3 next = EditorGUILayout.Vector3Field(label, current);
            if (clampPositive)
            {
                next.x = Mathf.Max(0.001f, next.x);
                next.y = Mathf.Max(0.001f, next.y);
                next.z = Mathf.Max(0.001f, next.z);
            }

            if (next == current)
            {
                return false;
            }

            if (data == null)
            {
                data = new Vector3Data();
            }

            data.x = next.x;
            data.y = next.y;
            data.z = next.z;
            return true;
        }

        private static Vector3 ReadVector3(Vector3Data data, Vector3 fallback)
        {
            if (data == null)
            {
                return fallback;
            }

            return new Vector3(data.x, data.y, data.z);
        }

        private static string ObjectTitle(ModuleObject moduleObject)
        {
            if (moduleObject == null)
            {
                return "<null object>";
            }

            return string.IsNullOrWhiteSpace(moduleObject.label)
                ? moduleObject.id
                : moduleObject.label + " (" + moduleObject.id + ")";
        }
    }
}
