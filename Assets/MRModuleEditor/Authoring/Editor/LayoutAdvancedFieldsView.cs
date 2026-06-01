using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class LayoutAdvancedFieldsView
    {
        private static readonly string[] FollowModes =
        {
            LayoutFollowModes.Fixed,
            LayoutFollowModes.FollowAnchor,
            LayoutFollowModes.SmoothFollow
        };

        private static readonly string[] ReadabilityProfiles =
        {
            LayoutReadabilityProfiles.Default,
            LayoutReadabilityProfiles.HeadPanel,
            LayoutReadabilityProfiles.WorldPanel,
            LayoutReadabilityProfiles.ObjectCallout,
            LayoutReadabilityProfiles.WorldObject
        };

        private static readonly string[] DeviceProfiles =
        {
            LayoutDeviceProfiles.Any,
            LayoutDeviceProfiles.Simulator,
            LayoutDeviceProfiles.Headset,
            LayoutDeviceProfiles.Desktop
        };

        public static bool Draw(LayoutDefinition layout)
        {
            if (layout == null)
            {
                return false;
            }

            bool changed = false;
            EditorGUILayout.LabelField("Advanced MR Layout", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            layout.faceUser = EditorGUILayout.Toggle("Face User", layout.faceUser);
            if (EditorGUI.EndChangeCheck()) changed = true;

            changed |= DrawPopup("Follow Mode", ref layout.followMode, FollowModes, LayoutFollowModes.Fixed);

            EditorGUI.BeginChangeCheck();
            float nextVisibilityRange = EditorGUILayout.FloatField("Visibility Range", layout.visibilityRange);
            nextVisibilityRange = Mathf.Max(0f, nextVisibilityRange);
            if (EditorGUI.EndChangeCheck())
            {
                layout.visibilityRange = nextVisibilityRange;
                changed = true;
            }

            changed |= DrawPopup("Readability Profile", ref layout.readabilityProfile, ReadabilityProfiles, LayoutReadabilityProfiles.Default);
            changed |= DrawPopup("Device Profile", ref layout.deviceProfile, DeviceProfiles, LayoutDeviceProfiles.Any);

            EditorGUILayout.HelpBox(
                "Face User rotates panels/callouts toward the viewer. Follow Mode controls whether object layouts stay fixed, update every frame, or smooth-follow their anchor. " +
                "Visibility Range of 0 means unlimited for now; readability/device profiles are validation hints for future target profiles.",
                MessageType.None);

            return changed;
        }

        private static bool DrawPopup(string label, ref string value, string[] options, string fallback)
        {
            if (options == null || options.Length == 0)
            {
                return false;
            }

            string current = string.IsNullOrWhiteSpace(value) ? fallback : value;
            int currentIndex = System.Array.IndexOf(options, current);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup(label, currentIndex, options);
            if (!EditorGUI.EndChangeCheck())
            {
                return false;
            }

            value = options[nextIndex];
            return true;
        }
    }
}
