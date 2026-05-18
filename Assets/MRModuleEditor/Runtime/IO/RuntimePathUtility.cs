using System.IO;

namespace MRModuleEditor.Runtime.IO
{
    public static class RuntimePathUtility
    {
        public static bool RequiresUnityWebRequest(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                return false;
            }

            string normalized = pathOrUrl.Replace("\\", "/");

            // Android StreamingAssets often look like jar:file://...!/assets/...
            // WebGL and some file URLs also contain ://.
            return normalized.Contains("://") || normalized.Contains("!") || normalized.StartsWith("jar:");
        }

        public static string CombinePathOrUrl(string root, string relative)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return relative ?? "";
            }

            if (string.IsNullOrWhiteSpace(relative))
            {
                return root;
            }

            if (Path.IsPathRooted(relative))
            {
                return relative;
            }

            string cleanRoot = root.Replace("\\", "/").TrimEnd('/');
            string cleanRelative = relative.Replace("\\", "/").TrimStart('/');
            return cleanRoot + "/" + cleanRelative;
        }

        public static string GetDirectoryPathOrUrl(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                return "";
            }

            string normalized = pathOrUrl.Replace("\\", "/");
            int lastSlash = normalized.LastIndexOf('/');
            if (lastSlash < 0)
            {
                return "";
            }

            return normalized.Substring(0, lastSlash);
        }

        public static string ResolveRelativeToModuleDirectory(string moduleDirectory, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return "";
            }

            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            return CombinePathOrUrl(moduleDirectory, assetPath);
        }
    }
}