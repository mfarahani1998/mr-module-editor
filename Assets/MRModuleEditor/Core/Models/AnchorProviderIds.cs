using System;

namespace MRModuleEditor.Core.Models
{
    public static class AnchorProviderIds
    {
        public const string Simulator = "simulator";
        public const string Manual = "manual";
        public const string Marker = "marker";
        public const string Spatial = "spatial";

        public static string Normalize(string provider, string anchorType)
        {
            if (!string.IsNullOrWhiteSpace(provider))
            {
                return provider;
            }

            // Keep existing modules working: blank provider means the built-in simulator/manual
            // transform path that AnchorResolver already used before Phase 5.
            if (anchorType == "world")
            {
                return Simulator;
            }

            return Simulator;
        }

        public static bool IsKnown(string provider)
        {
            string normalized = provider ?? "";
            return normalized == ""
                || normalized == Simulator
                || normalized == Manual
                || normalized == Marker
                || normalized == Spatial;
        }
    }
}
