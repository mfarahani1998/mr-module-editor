using System;
using System.Text.RegularExpressions;

namespace MRModuleEditor.Core.Utilities
{
    public static class IdGenerator
    {
        public static string NewId(string prefix)
        {
            string cleanPrefix = NormalizePrefix(prefix);
            string shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8);
            return cleanPrefix + "." + shortGuid;
        }

        public static string NormalizePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return "id";
            }

            string lower = prefix.Trim().ToLowerInvariant();
            string cleaned = Regex.Replace(lower, "[^a-z0-9_.-]+", "_");
            cleaned = cleaned.Trim('_', '.', '-');
            return string.IsNullOrEmpty(cleaned) ? "id" : cleaned;
        }
    }
}