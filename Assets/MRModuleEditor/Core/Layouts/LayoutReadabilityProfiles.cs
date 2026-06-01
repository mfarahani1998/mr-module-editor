namespace MRModuleEditor.Core.Layouts
{
    public static class LayoutReadabilityProfiles
    {
        public const string Default = "";
        public const string HeadPanel = "headPanel";
        public const string WorldPanel = "worldPanel";
        public const string ObjectCallout = "objectCallout";
        public const string WorldObject = "worldObject";

        public static bool IsKnown(string profile)
        {
            string normalized = profile ?? "";
            return normalized == Default
                || normalized == HeadPanel
                || normalized == WorldPanel
                || normalized == ObjectCallout
                || normalized == WorldObject;
        }
    }
}
