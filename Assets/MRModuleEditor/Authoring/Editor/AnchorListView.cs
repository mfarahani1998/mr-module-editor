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

        private static readonly string[] AnchorProviders =
        {
            "",
            AnchorProviderIds.Simulator,
            AnchorProviderIds.Manual,
            AnchorProviderIds.Marker,
            AnchorProviderIds.Spatial
        };

        private static readonly string[] CalibrationStatuses =
        {
            "",
            AnchorCalibrationStatuses.Unknown,
            AnchorCalibrationStatuses.Ready,
            AnchorCalibrationStatuses.Approximate,
            AnchorCalibrationStatuses.Lost
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

                anchor.displayName = EditorGUILayout.TextField("Display Name", anchor.displayName);
                anchor.provider = DrawStringPopup("Provider", anchor.provider, AnchorProviders);
                anchor.calibrationRequired = EditorGUILayout.Toggle("Calibration Required", anchor.calibrationRequired);
                anchor.calibrationStatus = DrawStringPopup("Calibration Status", anchor.calibrationStatus, CalibrationStatuses);

                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                }

                changed |= EditorIdDropdowns.DrawAnchorIdDropdown(
                    document,
                    ref anchor.fallbackAnchorId,
                    "Fallback Anchor",
                    true);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("Notes");
                anchor.notes = EditorGUILayout.TextArea(anchor.notes, GUILayout.MinHeight(38));
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

                string statusMessage;
                MessageType statusType;
                EditorModuleValidationUtility.TryGetAnchorAuthoringStatus(
                    document,
                    anchor,
                    out statusMessage,
                    out statusType);
                if (!string.IsNullOrWhiteSpace(statusMessage))
                {
                    EditorGUILayout.HelpBox(statusMessage, statusType);
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
                    targetObjectId = "",
                    provider = AnchorProviderIds.Simulator,
                    displayName = "Head Default",
                    calibrationStatus = AnchorCalibrationStatuses.Ready
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
                    targetObjectId = "",
                    provider = AnchorProviderIds.Simulator,
                    displayName = "World Default",
                    calibrationRequired = true,
                    calibrationStatus = AnchorCalibrationStatuses.Approximate,
                    fallbackAnchorId = "anchor.head.default"
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
                    targetObjectId = EditorModuleDataUtility.FirstObjectId(document),
                    provider = AnchorProviderIds.Simulator,
                    displayName = "Object Anchor",
                    fallbackAnchorId = "anchor.head.default"
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

        private static string DrawStringPopup(string label, string value, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                return EditorGUILayout.TextField(label, value ?? "");
            }

            string current = value ?? "";
            int currentIndex = System.Array.IndexOf(options, current);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = EditorGUILayout.Popup(label, currentIndex, options);
            nextIndex = Mathf.Clamp(nextIndex, 0, options.Length - 1);
            return options[nextIndex];
        }
    }
}