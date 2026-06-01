using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Utilities;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class LayoutInspectorView
    {
        public static bool Draw(ModuleDocument document, int selectedStepIndex)
        {
            EditorGUILayout.LabelField("Layout For Selected Step", EditorStyles.boldLabel);

            if (document == null || document.steps == null || document.steps.Count == 0)
            {
                EditorGUILayout.HelpBox("No step is available for layout editing.", MessageType.Info);
                return false;
            }

            if (selectedStepIndex < 0 || selectedStepIndex >= document.steps.Count)
            {
                EditorGUILayout.HelpBox("Select a step to edit its layout.", MessageType.Info);
                return false;
            }

            ModuleStep step = document.steps[selectedStepIndex];
            if (step == null)
            {
                EditorGUILayout.HelpBox("Selected step is null.", MessageType.Error);
                return false;
            }

            EditorModuleDataUtility.EnsureLists(document);

            int layoutCountForStep;
            LayoutDefinition layout = FindFirstLayoutForStep(document, step.id, out layoutCountForStep);

            if (layout == null)
            {
                EditorGUILayout.HelpBox(
                    "This step has no authored layout yet. The runtime will use the panel fallback offset. " +
                    "Create a layout if you want this step to be spatially tunable from module.json.",
                    MessageType.Info);

                if (GUILayout.Button("Create Layout For This Step"))
                {
                    document.layouts.Add(CreateDefaultLayout(document, step));
                    return true;
                }

                return false;
            }

            bool changed = false;

            EditorGUILayout.BeginVertical("box");

            if (layoutCountForStep > 1)
            {
                EditorGUILayout.HelpBox(
                    "More than one layout targets this step. The runtime resolver uses the first matching layout. " +
                    "Delete or retarget duplicates to avoid confusion.",
                    MessageType.Warning);
            }

            EditorGUI.BeginChangeCheck();
            layout.id = EditorGUILayout.TextField("Layout ID", layout.id);
            if (EditorGUI.EndChangeCheck()) changed = true;

            GUI.enabled = false;
            EditorGUILayout.TextField("Target Step", step.id);
            GUI.enabled = true;

            if (layout.targetId != step.id)
            {
                EditorGUILayout.HelpBox(
                    "This layout targetId does not match the selected step. This should not normally happen.",
                    MessageType.Warning);

                if (GUILayout.Button("Repair Target ID"))
                {
                    layout.targetId = step.id;
                    changed = true;
                }
            }

            changed |= EditorIdDropdowns.DrawAnchorIdDropdown(document, ref layout.anchorId, "Anchor");
            changed |= EditorLayoutPresetUtility.DrawPresetButtons(document, layout, LayoutPresetCatalog.StepTargetKind);

            Vector3 position = ReadVector3(layout.position, Vector3.zero);
            Vector3 nextPosition = EditorGUILayout.Vector3Field("Position", position);
            if (nextPosition != position)
            {
                WriteVector3Data(ref layout.position, nextPosition);
                changed = true;
            }

            Vector3 rotation = ReadVector3(layout.rotationEuler, Vector3.zero);
            Vector3 nextRotation = EditorGUILayout.Vector3Field("Rotation Euler", rotation);
            if (nextRotation != rotation)
            {
                WriteVector3Data(ref layout.rotationEuler, nextRotation);
                changed = true;
            }

            Vector3 scale = ReadVector3(layout.scale, Vector3.one);
            Vector3 nextScale = EditorGUILayout.Vector3Field("Scale", scale);
            if (nextScale != scale)
            {
                nextScale.x = Mathf.Max(0.001f, nextScale.x);
                nextScale.y = Mathf.Max(0.001f, nextScale.y);
                nextScale.z = Mathf.Max(0.001f, nextScale.z);
                WriteVector3Data(ref layout.scale, nextScale);
                changed = true;
            }

            changed |= LayoutAdvancedFieldsView.Draw(layout);

            EditorGUILayout.HelpBox(
                "Layout offsets are local to the selected anchor. Presets provide safe starting points, but you can still fine-tune x/y/z. " +
                "For head anchors, z usually stays 0 because AnchorResolver already applies the head distance. " +
                "Use scale for spatial size tuning.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Delete Layout For This Step"))
            {
                document.layouts.Remove(layout);
                EditorGUILayout.EndVertical();
                return true;
            }

            EditorGUILayout.EndVertical();
            return changed;
        }

        private static LayoutDefinition FindFirstLayoutForStep(
            ModuleDocument document,
            string stepId,
            out int count)
        {
            count = 0;
            LayoutDefinition first = null;

            if (document == null || document.layouts == null || string.IsNullOrWhiteSpace(stepId))
            {
                return null;
            }

            for (int i = 0; i < document.layouts.Count; i++)
            {
                LayoutDefinition layout = document.layouts[i];
                if (layout == null || layout.targetId != stepId)
                {
                    continue;
                }

                count++;
                if (first == null)
                {
                    first = layout;
                }
            }

            return first;
        }

        private static LayoutDefinition CreateDefaultLayout(ModuleDocument document, ModuleStep step)
        {
            string anchorId = step.GetString("anchorId", "");
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = EditorModuleDataUtility.FirstAnchorId(document, "head");
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = EditorModuleDataUtility.FirstAnchorId(document);
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = "anchor.head.default";
            }

            AnchorDefinition anchor = EditorModuleDataUtility.FindAnchor(document, anchorId);
            string anchorType = anchor == null ? "head" : anchor.type;

            Vector3 defaultPosition = DefaultPositionForAnchorType(anchorType);
            string cleanStepId = IdGenerator.NormalizePrefix(step.id);
            string layoutId = EditorModuleDataUtility.MakeUniqueId(
                document,
                "layout." + cleanStepId + ".default");

            return new LayoutDefinition
            {
                id = layoutId,
                targetId = step.id,
                anchorId = anchorId,
                position = new Vector3Data(defaultPosition.x, defaultPosition.y, defaultPosition.z),
                rotationEuler = new Vector3Data(0f, 0f, 0f),
                scale = new Vector3Data(1f, 1f, 1f),
                faceUser = true,
                followMode = anchorType == "object"
                    ? LayoutFollowModes.FollowAnchor
                    : anchorType == "head"
                        ? LayoutFollowModes.SmoothFollow
                        : LayoutFollowModes.Fixed,
                visibilityRange = anchorType == "object"
                    ? 2.5f
                    : anchorType == "world"
                        ? 4.5f
                        : 3.5f,
                readabilityProfile = anchorType == "object"
                    ? LayoutReadabilityProfiles.ObjectCallout
                    : anchorType == "world"
                        ? LayoutReadabilityProfiles.WorldPanel
                        : LayoutReadabilityProfiles.HeadPanel,
                deviceProfile = LayoutDeviceProfiles.Any
            };
        }

        private static Vector3 DefaultPositionForAnchorType(string anchorType)
        {
            if (anchorType == "object")
            {
                return new Vector3(0f, 0.75f, 0f);
            }

            if (anchorType == "world")
            {
                return Vector3.zero;
            }

            // Head anchor: headDistance already supplies physical distance.
            return new Vector3(0f, -0.15f, 0f);
        }

        private static Vector3 ReadVector3(Vector3Data data, Vector3 fallback)
        {
            if (data == null)
            {
                return fallback;
            }

            return new Vector3(data.x, data.y, data.z);
        }

        private static void WriteVector3Data(ref Vector3Data data, Vector3 value)
        {
            if (data == null)
            {
                data = new Vector3Data();
            }

            data.x = value.x;
            data.y = value.y;
            data.z = value.z;
        }
    }
}