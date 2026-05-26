using MRModuleEditor.Core.Models;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class AssetListView
    {
        public static bool Draw(ModuleDocument document)
        {
            if (document == null)
            {
                return false;
            }

            EditorModuleDataUtility.EnsureLists(document);
            bool changed = false;

            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

            for (int i = 0; i < document.assets.Count; i++)
            {
                ModuleAsset asset = document.assets[i];
                if (asset == null)
                {
                    asset = new ModuleAsset();
                    document.assets[i] = asset;
                    changed = true;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();

                asset.id = EditorGUILayout.TextField("ID", asset.id);
                asset.type = EditorGUILayout.TextField("Type", asset.type);
                asset.path = EditorGUILayout.TextField("Path", asset.path);
                asset.label = EditorGUILayout.TextField("Label", asset.label);

                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                }

                EditorGUILayout.HelpBox(
                    "Path is relative to the module folder, for example assets/images/intro.png. " +
                    "Do not use Unity asset GUIDs here.",
                    MessageType.None);

                bool remove = GUILayout.Button("Remove Asset");
                EditorGUILayout.EndVertical();

                if (remove)
                {
                    document.assets.RemoveAt(i);
                    return true;
                }
            }

            if (GUILayout.Button("Add Image Asset"))
            {
                document.assets.Add(new ModuleAsset
                {
                    id = EditorModuleDataUtility.MakeUniqueId(document, "asset.image"),
                    type = "image",
                    path = "assets/images/new_image.png",
                    label = "New Image"
                });
                return true;
            }

            if (GUILayout.Button("Add Audio Asset"))
            {
                document.assets.Add(new ModuleAsset
                {
                    id = EditorModuleDataUtility.MakeUniqueId(document, "asset.audio"),
                    type = "audio",
                    path = "assets/audio/new_audio.wav",
                    label = "New Audio"
                });
                return true;
            }

            return changed;
        }
    }
}