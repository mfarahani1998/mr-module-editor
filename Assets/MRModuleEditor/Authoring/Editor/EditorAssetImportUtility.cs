using System;
using System.Collections.Generic;
using System.IO;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Utilities;
using UnityEditor;

namespace MRModuleEditor.Authoring.Editor
{
    public static class EditorAssetImportUtility
    {
        private static readonly Dictionary<string, string[]> ExtensionsByType =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "image", new[] { ".png", ".jpg", ".jpeg", ".webp" } },
                { "audio", new[] { ".wav", ".mp3", ".ogg", ".aiff", ".aif" } },
                { "video", new[] { ".mp4", ".mov", ".webm", ".m4v" } }
            };

        public static bool TryImportAssetWithFilePanel(
            ModuleDocument document,
            string moduleJsonPath,
            string assetType,
            out ModuleAsset importedAsset,
            out string error)
        {
            importedAsset = null;
            error = "";

            string sourcePath = EditorUtility.OpenFilePanelWithFilters(
                "Import " + DisplayType(assetType),
                Directory.GetCurrentDirectory(),
                BuildFilters(assetType));

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                error = "Import cancelled.";
                return false;
            }

            return TryImportAssetFromSource(
                document,
                moduleJsonPath,
                sourcePath,
                assetType,
                "asset." + CleanType(assetType),
                out importedAsset,
                out error);
        }

        public static bool TryImportAssetFromSource(
            ModuleDocument document,
            string moduleJsonPath,
            string sourcePath,
            string assetType,
            string desiredIdPrefix,
            out ModuleAsset importedAsset,
            out string error)
        {
            importedAsset = null;
            error = "";

            if (document == null)
            {
                error = "No module document is loaded.";
                return false;
            }

            EditorModuleDataUtility.EnsureLists(document);

            if (!ModuleExportUtility.TryGetModuleFolder(moduleJsonPath, out string moduleFolder, out error))
            {
                error += "\n\nSave the module as a file named module.json before importing assets.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                error = "Source file does not exist: " + sourcePath;
                return false;
            }

            string cleanType = CleanType(assetType);
            if (!ExtensionMatchesType(sourcePath, cleanType))
            {
                error =
                    "The selected file extension does not look like a " + cleanType + " asset.\n\n" +
                    "File: " + sourcePath + "\n" +
                    "Allowed: " + string.Join(", ", GetAllowedExtensions(cleanType));
                return false;
            }

            string relativeFolder = RelativeFolderForType(cleanType);
            string destinationFolder = Path.Combine(moduleFolder, relativeFolder);
            Directory.CreateDirectory(destinationFolder);

            string destinationFileName = BuildUniqueFileName(destinationFolder, Path.GetFileName(sourcePath));
            string destinationPath = Path.Combine(destinationFolder, destinationFileName);

            string sourceFullPath = NormalizeFullPath(sourcePath);
            string destinationFullPath = NormalizeFullPath(destinationPath);

            if (!PathsAreSame(sourceFullPath, destinationFullPath))
            {
                File.Copy(sourceFullPath, destinationFullPath, true);
            }

            string relativePath = CombineRelative(relativeFolder, destinationFileName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationFileName);
            string idPrefix = string.IsNullOrWhiteSpace(desiredIdPrefix)
                ? "asset." + cleanType
                : desiredIdPrefix;
            string desiredId = idPrefix + "." + IdGenerator.NormalizePrefix(fileNameWithoutExtension);

            importedAsset = new ModuleAsset
            {
                id = EditorModuleDataUtility.MakeUniqueId(document, desiredId),
                type = cleanType,
                path = relativePath,
                label = MakeLabel(fileNameWithoutExtension)
            };

            document.assets.Add(importedAsset);
            ModuleExportUtility.RefreshAssetDatabaseIfInsideProject(destinationFullPath);
            return true;
        }

        public static bool TryResolveManagedAssetFilePath(
            string moduleJsonPath,
            ModuleAsset asset,
            out string absolutePath,
            out string error)
        {
            absolutePath = "";
            error = "";

            if (asset == null)
            {
                error = "Asset entry is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(asset.path))
            {
                error = "Asset path is empty.";
                return false;
            }

            if (LooksLikeUrl(asset.path))
            {
                error = "Asset path is a URL, not a local module file.";
                return false;
            }

            if (Path.IsPathRooted(asset.path))
            {
                error = "Asset path is absolute. For safety, this UI only deletes files with module-relative paths.";
                return false;
            }

            if (ContainsParentTraversal(asset.path))
            {
                error = "Asset path contains '..' and is not safe to delete from the editor.";
                return false;
            }

            if (!ModuleExportUtility.TryGetModuleFolder(moduleJsonPath, out string moduleFolder, out error))
            {
                error += "\n\nSave the module as a file named module.json before deleting copied assets.";
                return false;
            }

            string moduleAssetsFolder = NormalizeFullPath(Path.Combine(moduleFolder, "assets"));
            absolutePath = NormalizeFullPath(Path.Combine(moduleFolder, asset.path));

            if (!IsInsideFolder(absolutePath, moduleAssetsFolder))
            {
                error = "Only files inside this module's assets/ folder can be deleted from the asset list.";
                return false;
            }

            if (!File.Exists(absolutePath))
            {
                error = "Asset file does not exist on disk: " + absolutePath;
                return false;
            }

            return true;
        }

        public static bool TryDeleteManagedAssetFile(
            string moduleJsonPath,
            ModuleAsset asset,
            out string deletedPath,
            out string error)
        {
            deletedPath = "";
            error = "";

            string absolutePath;
            if (!TryResolveManagedAssetFilePath(moduleJsonPath, asset, out absolutePath, out error))
            {
                return false;
            }

            deletedPath = absolutePath;

            string projectRelativePath;
            if (TryGetProjectRelativePath(absolutePath, out projectRelativePath))
            {
                if (!AssetDatabase.DeleteAsset(projectRelativePath))
                {
                    error = "Unity AssetDatabase.DeleteAsset failed for: " + projectRelativePath;
                    return false;
                }
            }
            else
            {
                try
                {
                    File.Delete(absolutePath);
                    string metaPath = absolutePath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                    return false;
                }
            }

            TryPruneEmptyAssetFolders(moduleJsonPath, absolutePath);
            AssetDatabase.Refresh();
            return true;
        }

        public static bool IsKnownAssetType(string assetType)
        {
            return ExtensionsByType.ContainsKey(CleanType(assetType));
        }

        public static string[] GetAllowedExtensions(string assetType)
        {
            string[] extensions;
            return ExtensionsByType.TryGetValue(CleanType(assetType), out extensions)
                ? extensions
                : new string[0];
        }

        public static bool ExtensionMatchesType(string path, string assetType)
        {
            string[] extensions = GetAllowedExtensions(assetType);
            if (extensions.Length == 0)
            {
                return true;
            }

            string extension = Path.GetExtension(path);
            for (int i = 0; i < extensions.Length; i++)
            {
                if (string.Equals(extension, extensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] BuildFilters(string assetType)
        {
            string cleanType = CleanType(assetType);
            string[] extensions = GetAllowedExtensions(cleanType);
            string extensionList = extensions.Length == 0
                ? "*"
                : string.Join(",", StripDots(extensions));

            return new[]
            {
                DisplayType(cleanType) + " files",
                extensionList,
                "All files",
                "*"
            };
        }

        private static string[] StripDots(string[] extensions)
        {
            string[] result = new string[extensions.Length];
            for (int i = 0; i < extensions.Length; i++)
            {
                result[i] = (extensions[i] ?? "").TrimStart('.');
            }

            return result;
        }

        private static string RelativeFolderForType(string assetType)
        {
            string cleanType = CleanType(assetType);
            if (cleanType == "image") return "assets/images";
            if (cleanType == "audio") return "assets/audio";
            if (cleanType == "video") return "assets/video";
            return "assets/other";
        }

        private static string BuildUniqueFileName(string destinationFolder, string sourceFileName)
        {
            string safeName = string.IsNullOrWhiteSpace(sourceFileName)
                ? "asset"
                : sourceFileName;

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);
            string extension = Path.GetExtension(safeName);
            string candidate = safeName;
            int suffix = 2;

            while (File.Exists(Path.Combine(destinationFolder, candidate)))
            {
                candidate = nameWithoutExtension + "_" + suffix + extension;
                suffix++;
            }

            return candidate;
        }

        private static string CombineRelative(string folder, string fileName)
        {
            return (folder.TrimEnd('/', '\\') + "/" + fileName).Replace("\\", "/");
        }

        private static string MakeLabel(string fileNameWithoutExtension)
        {
            string clean = string.IsNullOrWhiteSpace(fileNameWithoutExtension)
                ? "Imported Asset"
                : fileNameWithoutExtension.Replace('_', ' ').Replace('-', ' ');

            return clean.Trim();
        }

        private static string DisplayType(string assetType)
        {
            string clean = CleanType(assetType);
            if (string.IsNullOrWhiteSpace(clean)) return "Asset";
            return char.ToUpperInvariant(clean[0]) + clean.Substring(1);
        }

        private static string CleanType(string assetType)
        {
            return string.IsNullOrWhiteSpace(assetType)
                ? "asset"
                : assetType.Trim().ToLowerInvariant();
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

        private static bool ContainsParentTraversal(string path)
        {
            string normalized = (path ?? "").Replace("\\", "/");
            string[] parts = normalized.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "..")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsideFolder(string filePath, string folderPath)
        {
            string normalizedFile = NormalizeFullPath(filePath);
            string normalizedFolder = NormalizeFullPath(folderPath);
            return normalizedFile.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetProjectRelativePath(string absolutePath, out string projectRelativePath)
        {
            projectRelativePath = "";
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return false;
            }

            string projectRoot = NormalizeFullPath(Directory.GetCurrentDirectory());
            string normalizedPath = NormalizeFullPath(absolutePath);
            if (!normalizedPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            projectRelativePath = normalizedPath.Substring(projectRoot.Length + 1);
            return projectRelativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryPruneEmptyAssetFolders(string moduleJsonPath, string deletedAssetPath)
        {
            string moduleFolder;
            string error;
            if (!ModuleExportUtility.TryGetModuleFolder(moduleJsonPath, out moduleFolder, out error))
            {
                return;
            }

            string moduleAssetsFolder = NormalizeFullPath(Path.Combine(moduleFolder, "assets"));
            string currentFolder = Path.GetDirectoryName(NormalizeFullPath(deletedAssetPath));

            while (!string.IsNullOrWhiteSpace(currentFolder)
                   && IsInsideFolder(currentFolder + "/placeholder", moduleAssetsFolder)
                   && !string.Equals(currentFolder, moduleAssetsFolder, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (Directory.Exists(currentFolder) && Directory.GetFileSystemEntries(currentFolder).Length == 0)
                    {
                        Directory.Delete(currentFolder);
                        string metaPath = currentFolder + ".meta";
                        if (File.Exists(metaPath))
                        {
                            File.Delete(metaPath);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch
                {
                    break;
                }

                currentFolder = Path.GetDirectoryName(currentFolder);
            }
        }

        private static string NormalizeFullPath(string path)
        {
            return Path.GetFullPath(path).Replace("\\", "/").TrimEnd('/');
        }

        private static bool PathsAreSame(string first, string second)
        {
            return string.Equals(
                NormalizeFullPath(first),
                NormalizeFullPath(second),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
