using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class EditorLayoutPresetUtility
    {
        public static bool DrawPresetButtons(ModuleDocument document, LayoutDefinition layout, string targetKind)
        {
            if (document == null || layout == null)
            {
                return false;
            }

            string anchorType = ResolveAnchorType(document, layout.anchorId);
            if (string.IsNullOrWhiteSpace(anchorType))
            {
                EditorGUILayout.HelpBox("Choose a known anchor before applying layout presets.", MessageType.Info);
                return false;
            }

            LayoutPresetDefinition[] presets = LayoutPresetCatalog.GetPresets(targetKind, anchorType);
            if (presets == null || presets.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No layout presets are available for target kind '" + targetKind + "' on a '" + anchorType + "' anchor yet.",
                    MessageType.None);
                return false;
            }

            EditorGUILayout.LabelField("Layout Presets", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "Presets only write position, rotation, and scale. They do not change the selected anchor or target id.",
                MessageType.None);

            bool changed = false;
            for (int i = 0; i < presets.Length; i++)
            {
                LayoutPresetDefinition preset = presets[i];
                if (preset == null)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply " + preset.displayName, GUILayout.Width(190)))
                {
                    LayoutPresetCatalog.ApplyPreset(layout, preset);
                    changed = true;
                }

                EditorGUILayout.LabelField(preset.description, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndHorizontal();
            }

            return changed;
        }

        private static string ResolveAnchorType(ModuleDocument document, string anchorId)
        {
            AnchorDefinition anchor = EditorModuleDataUtility.FindAnchor(document, anchorId);
            return anchor == null ? "" : anchor.type ?? "";
        }
    }
}
