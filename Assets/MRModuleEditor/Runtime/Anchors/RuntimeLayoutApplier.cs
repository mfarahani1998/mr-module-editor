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

                ApplyTransform(target.transform, anchorPose, layout);
            }
        }

        public static void ApplyTransform(Transform target, Pose anchorPose, LayoutDefinition layout)
        {
            if (target == null || layout == null)
            {
                return;
            }

            Vector3 localPosition = ToVector3(layout.position, Vector3.zero);
            Vector3 localEuler = ToVector3(layout.rotationEuler, Vector3.zero);
            Vector3 localScale = ToVector3(layout.scale, target.localScale);

            Quaternion localRotation = Quaternion.Euler(localEuler);
            target.position = anchorPose.position + anchorPose.rotation * localPosition;
            target.rotation = anchorPose.rotation * localRotation;
            target.localScale = localScale;
        }

        public static Vector3 ToVector3(Vector3Data data, Vector3 fallback)
        {
            if (data == null)
            {
                return fallback;
            }

            return new Vector3(data.x, data.y, data.z);
        }
    }
}