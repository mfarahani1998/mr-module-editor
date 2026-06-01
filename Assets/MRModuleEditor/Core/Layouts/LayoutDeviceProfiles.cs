namespace MRModuleEditor.Core.Layouts
{
    public static class LayoutDeviceProfiles
    {
        public const string Any = "";
        public const string Simulator = "simulator";
        public const string Headset = "headset";
        public const string Desktop = "desktop";

        public static bool IsKnown(string profile)
        {
            string normalized = profile ?? "";
            return normalized == Any
                || normalized == Simulator
                || normalized == Headset
                || normalized == Desktop;
        }
    }
}
