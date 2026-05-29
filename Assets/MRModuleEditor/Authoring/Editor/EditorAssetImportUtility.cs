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
