using System.IO;
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
            int assetIndexToRemove = -1;

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

                if (GUILayout.Button("Remove Asset"))
                {
                    assetIndexToRemove = i;
                }

                EditorGUILayout.EndVertical();
            }

            if (assetIndexToRemove >= 0 && assetIndexToRemove < document.assets.Count)
            {
                return TryRemoveAsset(document, moduleJsonPath, assetIndexToRemove) || changed;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Import Assets", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "Import copies a source file into this module folder and creates the ModuleAsset entry for you. " +
                "Save the module as module.json before importing.",
                MessageType.None);

            string importType = "";
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import Image..."))
            {
                importType = "image";
            }

            if (GUILayout.Button("Import Audio..."))
            {
                importType = "audio";
            }

            if (GUILayout.Button("Import Video..."))
            {
                importType = "video";
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(importType))
            {
                return TryImportAsset(document, moduleJsonPath, importType) || changed;
            }

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
            int assetCountBeforeImport = document == null || document.assets == null
                ? 0
                : document.assets.Count;

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
            int assetCountAfterImport = document == null || document.assets == null
                ? assetCountBeforeImport
                : document.assets.Count;

            if (assetCountAfterImport == assetCountBeforeImport)
            {
                EditorUtility.DisplayDialog(
                    "Asset Already Imported",
                    "This module already references: " + label,
                    "OK");
                return false;
            }

            EditorUtility.DisplayDialog("Asset Imported", "Imported asset: " + label, "OK");
            return true;
        }

        private static bool TryRemoveAsset(ModuleDocument document, string moduleJsonPath, int assetIndex)
        {
            ModuleAsset asset = document.assets[assetIndex];
            string assetLabel = BuildAssetLabel(asset, assetIndex);
            string resolvedPath;
            string resolveError;
            bool canDeleteFile = EditorAssetImportUtility.TryResolveManagedAssetFilePath(
                moduleJsonPath,
                asset,
                out resolvedPath,
                out resolveError);

            int choice;
            if (canDeleteFile)
            {
                choice = EditorUtility.DisplayDialogComplex(
                    "Remove Asset",
                    "Remove '" + assetLabel + "' from this module?\n\n" +
                    "You can remove only the module entry, or also delete the copied file from the module assets folder:\n" +
                    resolvedPath,
                    "Remove Entry Only",
                    "Cancel",
                    "Remove Entry And Delete File");
            }
            else
            {
                choice = EditorUtility.DisplayDialogComplex(
                    "Remove Asset",
                    "Remove '" + assetLabel + "' from this module?\n\n" +
                    "No safe module asset file was found to delete. " +
                    "Only files inside this module's assets/ folder can be removed from here.\n\n" +
                    "Details: " + resolveError,
                    "Remove Entry",
                    "Cancel",
                    "Keep");
            }

            if (choice == 1 || (choice == 2 && !canDeleteFile))
            {
                return false;
            }

            if (choice == 2 && canDeleteFile)
            {
                string deletedPath;
                string deleteError;
                if (!EditorAssetImportUtility.TryDeleteManagedAssetFile(
                        moduleJsonPath,
                        asset,
                        out deletedPath,
                        out deleteError))
                {
                    EditorUtility.DisplayDialog(
                        "Asset File Was Not Deleted",
                        "The module entry will be kept because the file could not be deleted.\n\n" + deleteError,
                        "OK");
                    return false;
                }
            }

            document.assets.RemoveAt(assetIndex);
            return true;
        }

        private static string BuildAssetLabel(ModuleAsset asset, int index)
        {
            if (asset == null)
            {
                return "Asset " + (index + 1);
            }

            if (!string.IsNullOrWhiteSpace(asset.label))
            {
                return asset.label;
            }

            if (!string.IsNullOrWhiteSpace(asset.id))
            {
                return asset.id;
            }

            if (!string.IsNullOrWhiteSpace(asset.path))
            {
                return Path.GetFileName(asset.path);
            }

            return "Asset " + (index + 1);
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
