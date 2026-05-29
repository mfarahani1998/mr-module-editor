using System;
using System.Collections.Generic;
using System.IO;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class ModuleExportUtility
    {
        public const string StreamingAssetsModuleRootRelative =
            "Assets/StreamingAssets/MRModuleEditor/SampleModules";

        [MenuItem("MR Module Editor/Export/Export Current Module Folder")]
        public static void ExportCurrentModuleFolderMenu()
        {
            if (!ModuleEditorWindow.TrySaveCurrentModuleForExport(
                    out string moduleJsonPath,
                    out string error))
            {
                EditorUtility.DisplayDialog("Cannot Export Module", error, "OK");
                return;
            }

            string defaultOutputRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "ModuleExports");

            Directory.CreateDirectory(defaultOutputRoot);

            string outputRoot = EditorUtility.OpenFolderPanel(
                "Choose Export Output Folder",
                defaultOutputRoot,
                "");

            if (string.IsNullOrWhiteSpace(outputRoot))
            {
                return;
            }

            string predictedTarget = BuildTargetFolderForRoot(moduleJsonPath, outputRoot);
            if (Directory.Exists(predictedTarget) && !ConfirmOverwrite(predictedTarget))
            {
                return;
            }

            if (!ConfirmMissingReferencedAssets(moduleJsonPath))
            {
                return;
            }

            if (!TryExportModuleFolderToRoot(
                    moduleJsonPath,
                    outputRoot,
                    true,
                    out string exportedFolder,
                    out error))
            {
                EditorUtility.DisplayDialog("Export Failed", error, "OK");
                return;
            }

            RefreshAssetDatabaseIfInsideProject(exportedFolder);

            Debug.Log("Exported module folder to: " + exportedFolder);
            EditorUtility.RevealInFinder(exportedFolder);
        }

        [MenuItem("MR Module Editor/Export/Export Current Module To StreamingAssets")]
        public static void ExportCurrentModuleToStreamingAssetsMenu()
        {
            if (!ModuleEditorWindow.TrySaveCurrentModuleForExport(
                    out string moduleJsonPath,
                    out string error))
            {
                EditorUtility.DisplayDialog("Cannot Export Module", error, "OK");
                return;
            }

            string targetFolder = BuildStreamingAssetsTargetFolder(moduleJsonPath);

            if (Directory.Exists(targetFolder) && !ConfirmOverwrite(targetFolder))
            {
                return;
            }

            if (!ConfirmMissingReferencedAssets(moduleJsonPath))
            {
                return;
            }

            if (!TryExportModuleFolderToExactTarget(
                    moduleJsonPath,
                    targetFolder,
                    true,
                    out error))
            {
                EditorUtility.DisplayDialog("StreamingAssets Export Failed", error, "OK");
                return;
            }

            RefreshAssetDatabaseIfInsideProject(targetFolder);

            string relativePath = BuildStreamingAssetsRelativeModulePath(moduleJsonPath);

            bool updatedSceneDefaults = false;
            string sceneUpdateMessage;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                updatedSceneDefaults = RuntimePreviewScenePathUtility
                    .PersistStreamingAssetsRelativeModulePathForPreviewScenes(
                        relativePath,
                        out string sceneUpdateError);

                sceneUpdateMessage = updatedSceneDefaults
                    ? "Updated RuntimePreview scene defaults."
                    : "Could not update RuntimePreview scene defaults: " + sceneUpdateError;
            }
            else
            {
                sceneUpdateMessage =
                    "Skipped updating RuntimePreview scene defaults because modified scene changes were not saved.";
            }

            Debug.Log(
                "Exported module to StreamingAssets: " + targetFolder +
                "\nRuntimeModuleLoader StreamingAssets relative path should be: " + relativePath);

            EditorUtility.DisplayDialog(
                "Exported To StreamingAssets",
                "Exported module to:\n\n" + targetFolder +
                "\nRuntimeModuleLoader StreamingAssets relative path: " + relativePath +
                "\n" + sceneUpdateMessage,
                "OK");
        }

        public static bool TryExportModuleFolderToRoot(
            string moduleJsonPath,
            string outputRootFolder,
            bool overwriteExistingFolder,
            out string exportedFolder,
            out string error)
        {
            exportedFolder = "";
            error = "";

            if (!TryGetModuleFolder(moduleJsonPath, out string sourceModuleFolder, out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(outputRootFolder))
            {
                error = "Output root folder is empty.";
                return false;
            }

            string moduleFolderName = GetLastFolderName(sourceModuleFolder);
            string targetModuleFolder = Path.Combine(outputRootFolder, moduleFolderName);

            exportedFolder = targetModuleFolder;

            return TryExportModuleFolderToExactTarget(
                moduleJsonPath,
                targetModuleFolder,
                overwriteExistingFolder,
                out error);
        }

        public static bool TryExportModuleFolderToExactTarget(
            string moduleJsonPath,
            string targetModuleFolder,
            bool overwriteExistingFolder,
            out string error)
        {
            error = "";

            if (!TryGetModuleFolder(moduleJsonPath, out string sourceModuleFolder, out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetModuleFolder))
            {
                error = "Target module folder is empty.";
                return false;
            }

            string sourceFullPath = NormalizeFullPath(sourceModuleFolder);
            string targetFullPath = NormalizeFullPath(targetModuleFolder);

            if (PathsAreSame(sourceFullPath, targetFullPath))
            {
                error = "Export target is the same as the source module folder.";
                return false;
            }

            if (IsPathInside(targetFullPath, sourceFullPath))
            {
                error =
                    "Export target is inside the source module folder. " +
                    "That would make the exporter copy its own output recursively.";
                return false;
            }

            if (IsPathInside(sourceFullPath, targetFullPath))
            {
                error =
                    "Export target contains the source module folder. " +
                    "Deleting or overwriting the target could delete the source.";
                return false;
            }

            if (Directory.Exists(targetFullPath))
            {
                if (!overwriteExistingFolder)
                {
                    error = "Target folder already exists: " + targetFullPath;
                    return false;
                }

                Directory.Delete(targetFullPath, true);
            }

            CopyDirectoryWithoutMetaFiles(sourceFullPath, targetFullPath);
            return true;
        }

        public static bool TryGetModuleFolder(
            string moduleJsonPath,
            out string moduleFolder,
            out string error)
        {
            moduleFolder = "";
            error = "";

            if (string.IsNullOrWhiteSpace(moduleJsonPath))
            {
                error = "Module JSON path is empty.";
                return false;
            }

            string fullPath = NormalizeFullPath(moduleJsonPath);

            if (!File.Exists(fullPath))
            {
                error = "Module JSON file does not exist: " + fullPath;
                return false;
            }

            string fileName = Path.GetFileName(fullPath);
            if (!string.Equals(fileName, "module.json", StringComparison.OrdinalIgnoreCase))
            {
                error = "Expected a file named module.json, but got: " + fullPath;
                return false;
            }

            moduleFolder = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(moduleFolder))
            {
                error = "Could not determine module folder for: " + fullPath;
                return false;
            }

            moduleFolder = NormalizeFullPath(moduleFolder);
            return true;
        }

        public static List<string> FindMissingReferencedAssetFiles(string moduleJsonPath)
        {
            List<string> missing = new List<string>();

            if (!TryGetModuleFolder(moduleJsonPath, out string moduleFolder, out string error))
            {
                missing.Add(error);
                return missing;
            }

            ModuleDocument document;
            try
            {
                document = ModuleJsonSerializer.LoadFromFile(moduleJsonPath);
            }
            catch (Exception ex)
            {
                missing.Add("Could not inspect module assets: " + ex.Message);
                return missing;
            }

            if (document.assets == null)
            {
                return missing;
            }

            for (int i = 0; i < document.assets.Count; i++)
            {
                ModuleAsset asset = document.assets[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.path))
                {
                    continue;
                }

                if (LooksLikeUrl(asset.path))
                {
                    continue;
                }

                string absoluteAssetPath = Path.IsPathRooted(asset.path)
                    ? asset.path
                    : Path.Combine(moduleFolder, asset.path);

                absoluteAssetPath = NormalizeFullPath(absoluteAssetPath);

                if (!File.Exists(absoluteAssetPath))
                {
                    string assetId = string.IsNullOrWhiteSpace(asset.id) ? "(missing asset id)" : asset.id;
                    missing.Add(assetId + " -> " + asset.path);
                }
            }

            return missing;
        }

        public static string BuildStreamingAssetsTargetFolder(string moduleJsonPath)
        {
            if (!TryGetModuleFolder(moduleJsonPath, out string moduleFolder, out string error))
            {
                throw new InvalidOperationException(error);
            }

            string moduleFolderName = GetLastFolderName(moduleFolder);

            return Path.Combine(
                Directory.GetCurrentDirectory(),
                StreamingAssetsModuleRootRelative,
                moduleFolderName);
        }

        public static string BuildStreamingAssetsRelativeModulePath(string moduleJsonPath)
        {
            if (!TryGetModuleFolder(moduleJsonPath, out string moduleFolder, out string error))
            {
                throw new InvalidOperationException(error);
            }

            string moduleFolderName = GetLastFolderName(moduleFolder);
            return ("MRModuleEditor/SampleModules/" + moduleFolderName + "/module.json")
                .Replace("\\", "/");
        }

        public static void RefreshAssetDatabaseIfInsideProject(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string projectRoot = NormalizeFullPath(Directory.GetCurrentDirectory());
            string fullPath = NormalizeFullPath(path);

            if (PathsAreSame(projectRoot, fullPath) || IsPathInside(fullPath, projectRoot))
            {
                AssetDatabase.Refresh();
            }
        }

        private static void CopyDirectoryWithoutMetaFiles(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
            {
                if (ShouldSkipFile(sourceFile))
                {
                    continue;
                }

                string fileName = Path.GetFileName(sourceFile);
                string targetFile = Path.Combine(targetDirectory, fileName);
                File.Copy(sourceFile, targetFile, true);
            }

            foreach (string sourceSubdirectory in Directory.GetDirectories(sourceDirectory))
            {
                string directoryName = Path.GetFileName(sourceSubdirectory);
                string targetSubdirectory = Path.Combine(targetDirectory, directoryName);
                CopyDirectoryWithoutMetaFiles(sourceSubdirectory, targetSubdirectory);
            }
        }

        private static bool ShouldSkipFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (string.Equals(extension, ".meta", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string fileName = Path.GetFileName(filePath);
            if (string.Equals(fileName, ".DS_Store", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileName, "Thumbs.db", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
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

        private static string GetLastFolderName(string folderPath)
        {
            string clean = NormalizeFullPath(folderPath).TrimEnd('/');
            return Path.GetFileName(clean);
        }

        private static string NormalizeFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            return Path.GetFullPath(path).Replace("\\", "/").TrimEnd('/');
        }

        private static bool PathsAreSame(string first, string second)
        {
            return string.Equals(
                NormalizeFullPath(first),
                NormalizeFullPath(second),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPathInside(string possibleChild, string possibleParent)
        {
            string child = NormalizeFullPath(possibleChild);
            string parent = NormalizeFullPath(possibleParent);

            if (string.IsNullOrWhiteSpace(child) || string.IsNullOrWhiteSpace(parent))
            {
                return false;
            }

            parent = parent.TrimEnd('/') + "/";
            child = child.TrimEnd('/') + "/";

            return child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildTargetFolderForRoot(string moduleJsonPath, string outputRoot)
        {
            if (!TryGetModuleFolder(moduleJsonPath, out string moduleFolder, out string error))
            {
                throw new InvalidOperationException(error);
            }

            string moduleFolderName = GetLastFolderName(moduleFolder);
            return Path.Combine(outputRoot, moduleFolderName);
        }

        private static bool ConfirmOverwrite(string targetFolder)
        {
            return EditorUtility.DisplayDialog(
                "Overwrite Existing Export?",
                "The export target already exists:\n\n" + targetFolder +
                "\n\nOverwrite it?",
                "Overwrite",
                "Cancel");
        }

        private static bool ConfirmMissingReferencedAssets(string moduleJsonPath)
        {
            List<string> missing = FindMissingReferencedAssetFiles(moduleJsonPath);

            if (missing.Count == 0)
            {
                return true;
            }

            int shownCount = Math.Min(missing.Count, 12);
            string[] shown = new string[shownCount];
            for (int i = 0; i < shownCount; i++)
            {
                shown[i] = missing[i];
            }

            string message =
                "The module references files that do not exist on disk:\n\n" +
                string.Join("\n", shown);

            if (missing.Count > shownCount)
            {
                message += "\n...and " + (missing.Count - shownCount) + " more.";
            }

            message +=
                "\n\nExporting anyway may produce a module that loads JSON but fails during image/audio steps.";

            return EditorUtility.DisplayDialog(
                "Missing Referenced Asset Files",
                message,
                "Export Anyway",
                "Cancel");
        }
    }
}