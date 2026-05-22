using MRModuleEditor.Core.Models;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class AnchorListView
    {
        private static readonly string[] AnchorTypes =
        {
            "head",
            "world",
            "object"
        };

        public static bool Draw(ModuleDocument document)
        {
            if (document == null)
            {
                return false;
            }

            EditorModuleDataUtility.EnsureLists(document);
            bool changed = false;

            EditorGUILayout.LabelField("Anchors", EditorStyles.boldLabel);

            for (int i = 0; i < document.anchors.Count; i++)
            {
                AnchorDefinition anchor = document.anchors[i];
                if (anchor == null)
                {
                    anchor = new AnchorDefinition();
                    document.anchors[i] = anchor;
                    changed = true;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();

                anchor.id = EditorGUILayout.TextField("ID", anchor.id);

                int currentTypeIndex = System.Array.IndexOf(AnchorTypes, anchor.type);
                if (currentTypeIndex < 0)
                {
                    currentTypeIndex = 0;
                }

                int nextTypeIndex = EditorGUILayout.Popup("Type", currentTypeIndex, AnchorTypes);
                anchor.type = AnchorTypes[nextTypeIndex];

                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                }

                if (anchor.type == "object")
                {
                    changed |= EditorIdDropdowns.DrawObjectIdDropdown(
                        document,
                        ref anchor.targetObjectId,
                        "Target Object");
                }
                else if (!string.IsNullOrWhiteSpace(anchor.targetObjectId))
                {
                    anchor.targetObjectId = "";
                    changed = true;
                }

                bool remove = GUILayout.Button("Remove Anchor");
                EditorGUILayout.EndVertical();

                if (remove)
                {
                    document.anchors.RemoveAt(i);
                    return true;
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Head Anchor"))
            {
                document.anchors.Add(new AnchorDefinition
                {
                    id = EditorModuleDataUtility.MakeUniqueId(document, "anchor.head.default"),
                    type = "head",
                    targetObjectId = ""
                });
                EditorGUILayout.EndHorizontal();
                return true;
            }

            if (GUILayout.Button("Add World Anchor"))
            {
                document.anchors.Add(new AnchorDefinition
                {
                    id = EditorModuleDataUtility.MakeUniqueId(document, "anchor.world.default"),
                    type = "world",
                    targetObjectId = ""
                });
                EditorGUILayout.EndHorizontal();
                return true;
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = !string.IsNullOrWhiteSpace(EditorModuleDataUtility.FirstObjectId(document));
            if (GUILayout.Button("Add Object Anchor"))
            {
                document.anchors.Add(new AnchorDefinition
                {
                    id = EditorModuleDataUtility.MakeUniqueId(document, "anchor.object"),
                    type = "object",
                    targetObjectId = EditorModuleDataUtility.FirstObjectId(document)
                });
                GUI.enabled = true;
                return true;
            }
            GUI.enabled = true;

            if (string.IsNullOrWhiteSpace(EditorModuleDataUtility.FirstObjectId(document)))
            {
                EditorGUILayout.HelpBox("Create a module object before adding an object anchor.", MessageType.Info);
            }

            return changed;
        }
    }
}