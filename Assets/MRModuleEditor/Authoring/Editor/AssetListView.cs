using MRModuleEditor.Core.Models;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class AssetListView
    {
        public static bool Draw(ModuleDocument document, string moduleJsonPath = "")
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

                DrawPerAssetHints(asset, moduleJsonPath);

                bool remove = GUILayout.Button("Remove Asset");
                EditorGUILayout.EndVertical();

                if (remove)
                {
                    document.assets.RemoveAt(i);
                    return true;
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Import Assets", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "Import copies a source file into this module folder and creates the ModuleAsset entry for you. " +
                "Save the module as module.json before importing.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import Image..."))
            {
                return TryImportAsset(document, moduleJsonPath, "image");
            }

            if (GUILayout.Button("Import Audio..."))
            {
                return TryImportAsset(document, moduleJsonPath, "audio");
            }

            if (GUILayout.Button("Import Video..."))
            {
                return TryImportAsset(document, moduleJsonPath, "video");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Manual Asset Entries", EditorStyles.miniBoldLabel);

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

            if (GUILayout.Button("Add Video Asset"))
            {
                document.assets.Add(new ModuleAsset
                {
                    id = EditorModuleDataUtility.MakeUniqueId(document, "asset.video"),
                    type = "video",
                    path = "assets/video/new_video.mp4",
                    label = "New Video"
                });
                return true;
            }

            return changed;
        }

        private static bool TryImportAsset(ModuleDocument document, string moduleJsonPath, string assetType)
        {
            ModuleAsset importedAsset;
            string error;
            if (!EditorAssetImportUtility.TryImportAssetWithFilePanel(
                    document,
                    moduleJsonPath,
                    assetType,
                    out importedAsset,
                    out error))
            {
                if (!string.IsNullOrWhiteSpace(error) && error != "Import cancelled.")
                {
                    EditorUtility.DisplayDialog("Import Asset Failed", error, "OK");
                }

                return false;
            }

            string label = importedAsset == null ? assetType : importedAsset.label;
            EditorUtility.DisplayDialog("Asset Imported", "Imported asset: " + label, "OK");
            return true;
        }

        private static void DrawPerAssetHints(ModuleAsset asset, string moduleJsonPath)
        {
            if (asset == null)
            {
                return;
            }

            if (EditorAssetImportUtility.IsKnownAssetType(asset.type)
                && !EditorAssetImportUtility.ExtensionMatchesType(asset.path, asset.type))
            {
                EditorGUILayout.HelpBox(
                    "This path extension does not look like an " + asset.type + " file. " +
                    "Validation will warn about this.",
                    MessageType.Warning);
            }

            if (string.IsNullOrWhiteSpace(moduleJsonPath))
            {
                EditorGUILayout.HelpBox(
                    "Save the module before relying on asset file checks or using Import.",
                    MessageType.None);
            }
        }
    }
}
