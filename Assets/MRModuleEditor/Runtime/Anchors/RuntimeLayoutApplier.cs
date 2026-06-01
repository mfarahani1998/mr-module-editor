using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.SceneBinding;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public class RuntimeLayoutApplier : MonoBehaviour
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private SceneBindingRegistry sceneBindingRegistry;

        private void Awake()
        {
            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            if (sceneBindingRegistry == null)
            {
                sceneBindingRegistry = FindFirstObjectByType<SceneBindingRegistry>();
            }
        }

        public void ApplyObjectLayouts(ModuleDocument module)
        {
            if (module == null || module.layouts == null)
            {
                return;
            }

            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            if (sceneBindingRegistry == null)
            {
                sceneBindingRegistry = FindFirstObjectByType<SceneBindingRegistry>();
            }

            if (anchorResolver == null || sceneBindingRegistry == null)
            {
                Debug.LogWarning("RuntimeLayoutApplier cannot apply layouts because AnchorResolver or SceneBindingRegistry is missing.");
                return;
            }

            for (int i = 0; i < module.layouts.Count; i++)
            {
                LayoutDefinition layout = module.layouts[i];
                if (layout == null || string.IsNullOrWhiteSpace(layout.targetId))
                {
                    continue;
                }

                if (!layout.targetId.StartsWith("object."))
                {
                    continue;
                }

                GameObject target;
                string objectError;
                if (!sceneBindingRegistry.TryGetObjectByModuleObjectId(
                        module,
                        layout.targetId,
                        out target,
                        out objectError))
                {
                    Debug.LogWarning("Could not apply layout '" + layout.id + "': " + objectError);
                    continue;
                }

                Pose anchorPose;
                string anchorError;
                if (!anchorResolver.TryResolveAnchor(module, layout.anchorId, out anchorPose, out anchorError))
                {
                    Debug.LogWarning("Could not apply layout '" + layout.id + "': " + anchorError);
                    continue;
                }

                ApplyTransform(target.transform, anchorPose, layout, anchorResolver.ViewerCamera);
                ConfigureFollowerIfNeeded(target, module, layout);
            }
        }

        public static void ApplyTransform(Transform target, Pose anchorPose, LayoutDefinition layout)
        {
            ApplyTransform(target, anchorPose, layout, null);
        }

        public static void ApplyTransform(Transform target, Pose anchorPose, LayoutDefinition layout, Camera viewerCamera)
        {
            bool hasAppliedPose = false;
            ApplyTransform(
                target,
                anchorPose,
                layout,
                viewerCamera,
                false,
                0f,
                0f,
                ref hasAppliedPose);
        }

        public static void ApplyTransform(
            Transform target,
            Pose anchorPose,
            LayoutDefinition layout,
            Camera viewerCamera,
            bool smooth,
            float smoothFollowSharpness,
            float snapDistance,
            ref bool hasAppliedPose)
        {
            if (target == null || layout == null)
            {
                return;
            }

            Vector3 localPosition = ToVector3(layout.position, Vector3.zero);
            Vector3 localEuler = ToVector3(layout.rotationEuler, Vector3.zero);
            Vector3 localScale = ToVector3(layout.scale, target.localScale);

            Quaternion localRotation = Quaternion.Euler(localEuler);
            Vector3 worldPosition = anchorPose.position + anchorPose.rotation * localPosition;
            Quaternion worldRotation = anchorPose.rotation * localRotation;
            if (layout.faceUser)
            {
                Quaternion facingRotation = AnchorResolver.GetCameraFacingRotation(viewerCamera, worldPosition, anchorPose.rotation);
                worldRotation = facingRotation * localRotation;
            }

            target.localScale = localScale;

            if (!smooth || !Application.isPlaying || !hasAppliedPose)
            {
                target.position = worldPosition;
                target.rotation = worldRotation;
                hasAppliedPose = true;
                return;
            }

            float sharpness = Mathf.Max(0.01f, smoothFollowSharpness);
            float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            if (snapDistance > 0f && Vector3.Distance(target.position, worldPosition) > snapDistance)
            {
                target.position = worldPosition;
            }
            else
            {
                target.position = Vector3.Lerp(target.position, worldPosition, t);
            }

            target.rotation = Quaternion.Slerp(target.rotation, worldRotation, t);
        }

        public static Vector3 ToVector3(Vector3Data data, Vector3 fallback)
        {
            if (data == null)
            {
                return fallback;
            }

            return new Vector3(data.x, data.y, data.z);
        }

        private void ConfigureFollowerIfNeeded(GameObject target, ModuleDocument module, LayoutDefinition layout)
        {
            if (target == null || layout == null)
            {
                return;
            }

            bool shouldFollow = LayoutFollowModes.NeedsFollower(layout.followMode) || layout.faceUser;
            RuntimeLayoutFollower follower = target.GetComponent<RuntimeLayoutFollower>();
            if (!shouldFollow)
            {
                if (follower != null)
                {
                    follower.Configure(null, null, null);
                }

                return;
            }

            if (follower == null)
            {
                follower = target.AddComponent<RuntimeLayoutFollower>();
            }

            follower.Configure(module, layout, anchorResolver);
        }
    }
}
