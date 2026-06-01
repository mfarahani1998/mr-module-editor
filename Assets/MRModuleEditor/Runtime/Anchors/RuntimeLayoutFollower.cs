using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public class RuntimeLayoutFollower : MonoBehaviour
    {
        [SerializeField]
        private float smoothFollowSharpness = 10f;

        [SerializeField]
        private float snapDistance = 1.25f;

        private ModuleDocument module;
        private LayoutDefinition layout;
        private AnchorResolver anchorResolver;
        private bool hasAppliedPose;

        public void Configure(ModuleDocument moduleDocument, LayoutDefinition layoutDefinition, AnchorResolver resolver)
        {
            module = moduleDocument;
            layout = layoutDefinition;
            anchorResolver = resolver;
            hasAppliedPose = false;

            enabled = layout != null
                && (LayoutFollowModes.NeedsFollower(layout.followMode) || layout.faceUser);
        }

        private void LateUpdate()
        {
            if (module == null || layout == null || anchorResolver == null)
            {
                enabled = false;
                return;
            }

            Pose anchorPose;
            string error;
            if (!anchorResolver.TryResolveAnchor(module, layout.anchorId, out anchorPose, out error))
            {
                return;
            }

            RuntimeLayoutApplier.ApplyTransform(
                transform,
                anchorPose,
                layout,
                anchorResolver.ViewerCamera,
                LayoutFollowModes.Normalize(layout.followMode) == LayoutFollowModes.SmoothFollow,
                smoothFollowSharpness,
                snapDistance,
                ref hasAppliedPose);
        }
    }
}
