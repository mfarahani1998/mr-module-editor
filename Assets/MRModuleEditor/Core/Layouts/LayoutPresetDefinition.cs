using System;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Core.Layouts
{
    [Serializable]
    public class LayoutPresetDefinition
    {
        public string id = "";
        public string displayName = "";
        public string description = "";
        public string targetKind = "";
        public string anchorType = "";
        public Vector3Data position = new Vector3Data();
        public Vector3Data rotationEuler = new Vector3Data();
        public Vector3Data scale = new Vector3Data(1f, 1f, 1f);
        public bool faceUser = false;
        public string followMode = "";
        public float visibilityRange = 0f;
        public string readabilityProfile = "";
        public string deviceProfile = "";

        public LayoutPresetDefinition()
        {
        }

        public LayoutPresetDefinition(
            string id,
            string displayName,
            string description,
            string targetKind,
            string anchorType,
            Vector3Data position,
            Vector3Data rotationEuler,
            Vector3Data scale)
            : this(
                id,
                displayName,
                description,
                targetKind,
                anchorType,
                position,
                rotationEuler,
                scale,
                false,
                LayoutFollowModes.Fixed,
                0f,
                LayoutReadabilityProfiles.Default,
                LayoutDeviceProfiles.Any)
        {
        }

        public LayoutPresetDefinition(
            string id,
            string displayName,
            string description,
            string targetKind,
            string anchorType,
            Vector3Data position,
            Vector3Data rotationEuler,
            Vector3Data scale,
            bool faceUser,
            string followMode,
            float visibilityRange,
            string readabilityProfile,
            string deviceProfile)
        {
            this.id = id ?? "";
            this.displayName = displayName ?? "";
            this.description = description ?? "";
            this.targetKind = targetKind ?? "";
            this.anchorType = anchorType ?? "";
            this.position = Clone(position, new Vector3Data());
            this.rotationEuler = Clone(rotationEuler, new Vector3Data());
            this.scale = Clone(scale, new Vector3Data(1f, 1f, 1f));
            this.faceUser = faceUser;
            this.followMode = string.IsNullOrWhiteSpace(followMode) ? LayoutFollowModes.Fixed : followMode;
            this.visibilityRange = Math.Max(0f, visibilityRange);
            this.readabilityProfile = readabilityProfile ?? "";
            this.deviceProfile = deviceProfile ?? "";
        }

        public LayoutPresetDefinition Clone()
        {
            return new LayoutPresetDefinition(
                id,
                displayName,
                description,
                targetKind,
                anchorType,
                position,
                rotationEuler,
                scale,
                faceUser,
                followMode,
                visibilityRange,
                readabilityProfile,
                deviceProfile);
        }

        internal static Vector3Data Clone(Vector3Data source, Vector3Data fallback)
        {
            Vector3Data value = source ?? fallback ?? new Vector3Data();
            return new Vector3Data(value.x, value.y, value.z);
        }
    }
}
