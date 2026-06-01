namespace MRModuleEditor.Core.Models
{
    public static class AnchorCalibrationStatuses
    {
        public const string Unknown = "unknown";
        public const string Ready = "ready";
        public const string Approximate = "approximate";
        public const string Lost = "lost";

        public static string Normalize(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? Unknown : status;
        }

        public static bool IsKnown(string status)
        {
            string normalized = Normalize(status);
            return normalized == Unknown
                || normalized == Ready
                || normalized == Approximate
                || normalized == Lost;
        }

        public static bool IsUsable(string status)
        {
            string normalized = Normalize(status);
            return normalized == Ready || normalized == Approximate;
        }
    }
}
