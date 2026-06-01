namespace MRModuleEditor.Core.Layouts
{
    public static class LayoutFollowModes
    {
        public const string Fixed = "fixed";
        public const string FollowAnchor = "followAnchor";
        public const string SmoothFollow = "smoothFollow";

        public static string Normalize(string followMode)
        {
            return string.IsNullOrWhiteSpace(followMode) ? Fixed : followMode;
        }

        public static bool IsKnown(string followMode)
        {
            string normalized = Normalize(followMode);
            return normalized == Fixed
                || normalized == FollowAnchor
                || normalized == SmoothFollow;
        }

        public static bool NeedsFollower(string followMode)
        {
            string normalized = Normalize(followMode);
            return normalized == FollowAnchor || normalized == SmoothFollow;
        }
    }
}
