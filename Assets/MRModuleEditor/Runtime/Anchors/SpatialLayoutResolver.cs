using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public class SpatialLayoutResolver : MonoBehaviour
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        private void Awake()
        {
            EnsureAnchorResolver();
        }

        public bool TryResolvePoseForTarget(
            ModuleDocument module,
            string targetId,
            string fallbackAnchorId,
            Vector3 fallbackLocalOffset,
            Vector3 fallbackLocalEuler,
            Vector3 fallbackLocalScale,
            out Pose pose,
            out Vector3 scale,
            out string error)
        {
            return TryResolvePoseForTarget(
                module,
                targetId,
                fallbackAnchorId,
                fallbackLocalOffset,
                fallbackLocalEuler,
                fallbackLocalScale,
                false,
                out pose,
                out scale,
                out error);
        }

        public bool TryResolvePoseForTarget(
            ModuleDocument module,
            string targetId,
            string fallbackAnchorId,
            Vector3 fallbackLocalOffset,
            Vector3 fallbackLocalEuler,
            Vector3 fallbackLocalScale,
            bool applyFallbackOffsetToAuthoredLayout,
            out Pose pose,
            out Vector3 scale,
            out string error)
        {
            pose = new Pose(Vector3.zero, Quaternion.identity);
            scale = fallbackLocalScale;
            error = "";

            if (module == null)
            {
                error = "ModuleDocument is null.";
                return false;
            }

            EnsureAnchorResolver();
            if (anchorResolver == null)
            {
                error = "AnchorResolver is missing.";
                return false;
            }

            LayoutDefinition layout = FindLayoutForTarget(module, targetId);
            string anchorId = ResolveAnchorId(null, layout, fallbackAnchorId);

            Pose anchorPose;
            if (!anchorResolver.TryResolveAnchor(module, anchorId, out anchorPose, out error))
            {
                return false;
            }

            Vector3 localPosition = layout == null
                ? fallbackLocalOffset
                : RuntimeLayoutApplier.ToVector3(layout.position, Vector3.zero);

            if (layout != null && applyFallbackOffsetToAuthoredLayout)
            {
                localPosition += fallbackLocalOffset;
            }

            Vector3 localEuler = layout == null
                ? fallbackLocalEuler
                : RuntimeLayoutApplier.ToVector3(layout.rotationEuler, Vector3.zero);

            scale = layout == null
                ? fallbackLocalScale
                : RuntimeLayoutApplier.ToVector3(layout.scale, fallbackLocalScale);

            Quaternion localRotation = Quaternion.Euler(localEuler);
            Vector3 worldPosition = anchorPose.position + anchorPose.rotation * localPosition;
            Quaternion worldRotation = anchorPose.rotation * localRotation;

            if (layout != null && layout.faceUser)
            {
                Quaternion facingRotation = AnchorResolver.GetCameraFacingRotation(anchorResolver.ViewerCamera, worldPosition, anchorPose.rotation);
                worldRotation = facingRotation * localRotation;
            }

            pose = new Pose(worldPosition, worldRotation);

            return true;
        }

        public bool TryResolvePoseForStep(
            ModuleDocument module,
            ModuleStep step,
            string fallbackAnchorId,
            Vector3 fallbackLocalOffset,
            Vector3 fallbackLocalEuler,
            Vector3 fallbackLocalScale,
            bool applyFallbackOffsetToAuthoredLayout,
            out Pose pose,
            out Vector3 scale,
            out string error)
        {
            string targetId = step == null ? "" : step.id;
            LayoutDefinition layout = FindLayoutForTarget(module, targetId);
            string anchorId = ResolveAnchorId(step, layout, fallbackAnchorId);

            return TryResolvePoseForTarget(
                module,
                targetId,
                anchorId,
                fallbackLocalOffset,
                fallbackLocalEuler,
                fallbackLocalScale,
                applyFallbackOffsetToAuthoredLayout,
                out pose,
                out scale,
                out error);
        }

        public LayoutDefinition FindLayoutForTarget(ModuleDocument module, string targetId)
        {
            if (module == null || module.layouts == null || string.IsNullOrWhiteSpace(targetId))
            {
                return null;
            }

            for (int i = 0; i < module.layouts.Count; i++)
            {
                LayoutDefinition layout = module.layouts[i];
                if (layout != null && layout.targetId == targetId)
                {
                    return layout;
                }
            }

            return null;
        }

        public string ResolveAnchorId(ModuleStep step, LayoutDefinition layout, string fallbackAnchorId)
        {
            string anchorId = layout == null ? "" : layout.anchorId;

            if (string.IsNullOrWhiteSpace(anchorId) && step != null)
            {
                anchorId = step.GetString("anchorId", "");
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = fallbackAnchorId;
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = "anchor.head.default";
            }

            return anchorId;
        }

        public bool IsStepHeadAnchored(ModuleDocument module, ModuleStep step, string fallbackAnchorId)
        {
            if (module == null || step == null)
            {
                return false;
            }

            LayoutDefinition layout = FindLayoutForTarget(module, step.id);
            string anchorId = ResolveAnchorId(step, layout, fallbackAnchorId);
            AnchorDefinition anchor = FindAnchor(module, anchorId);
            return anchor != null && anchor.type == "head";
        }

        public AnchorDefinition FindAnchor(ModuleDocument module, string anchorId)
        {
            if (module == null || module.anchors == null || string.IsNullOrWhiteSpace(anchorId))
            {
                return null;
            }

            for (int i = 0; i < module.anchors.Count; i++)
            {
                AnchorDefinition anchor = module.anchors[i];
                if (anchor != null && anchor.id == anchorId)
                {
                    return anchor;
                }
            }

            return null;
        }

        private void EnsureAnchorResolver()
        {
            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }
        }
    }
}