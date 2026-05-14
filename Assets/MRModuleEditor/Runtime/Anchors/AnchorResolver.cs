using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.SceneBinding;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public class AnchorResolver : MonoBehaviour
    {
        [SerializeField]
        private Camera viewerCamera;

        [SerializeField]
        private SceneBindingRegistry sceneBindingRegistry;

        [SerializeField]
        private Transform simulatorWorldOrigin;

        [Header("Head Anchor")]
        [SerializeField]
        private float headDistance = 10f;

        // For a subtitle-style panel, keep this centered and slightly below the user's view.
        // X = right, Y = up, Z = extra forward, in viewer-camera space.
        [SerializeField]
        private Vector3 headOffset = new Vector3(0f, -0.75f, 0f);

        [Header("World Anchor")]
        [SerializeField]
        private float defaultWorldDistance = 2f;

        public Camera ViewerCamera
        {
            get
            {
                if (viewerCamera == null)
                {
                    viewerCamera = Camera.main;
                }

                return viewerCamera;
            }
        }

        private void Awake()
        {
            if (viewerCamera == null)
            {
                viewerCamera = Camera.main;
            }

            if (sceneBindingRegistry == null)
            {
                sceneBindingRegistry = FindFirstObjectByType<SceneBindingRegistry>();
            }

            if (simulatorWorldOrigin == null)
            {
                simulatorWorldOrigin = transform;
            }
        }

        [ContextMenu("Recenter Simulator World Origin")]
        public void RecenterSimulatorWorldOrigin()
        {
            Camera camera = ViewerCamera;
            if (camera == null || simulatorWorldOrigin == null)
            {
                Debug.LogWarning("Cannot recenter simulator origin because the camera or origin is missing.");
                return;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            flatForward.Normalize();
            simulatorWorldOrigin.position = camera.transform.position + flatForward * defaultWorldDistance;
            simulatorWorldOrigin.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }

        public bool TryResolveAnchor(
            ModuleDocument module,
            string anchorId,
            out Pose pose,
            out string error)
        {
            pose = new Pose(Vector3.zero, Quaternion.identity);
            error = "";

            if (module == null)
            {
                error = "ModuleDocument is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = "anchor.head.default";
            }

            AnchorDefinition anchor = FindAnchor(module, anchorId);
            if (anchor == null)
            {
                error = "No anchor with id '" + anchorId + "' exists in the module.";
                return false;
            }

            if (anchor.type == "head")
            {
                return TryResolveHeadAnchor(out pose, out error);
            }

            if (anchor.type == "world")
            {
                Transform origin = simulatorWorldOrigin == null ? transform : simulatorWorldOrigin;
                pose = new Pose(origin.position, origin.rotation);
                return true;
            }

            if (anchor.type == "object")
            {
                return TryResolveObjectAnchor(module, anchor, out pose, out error);
            }

            error = "Unsupported anchor type '" + anchor.type + "' for anchor '" + anchorId + "'.";
            return false;
        }

        private bool TryResolveHeadAnchor(out Pose pose, out string error)
        {
            pose = new Pose(Vector3.zero, Quaternion.identity);
            error = "";

            Camera camera = ViewerCamera;
            if (camera == null)
            {
                error = "No Main Camera found for head anchor.";
                return false;
            }

            Transform cameraTransform = camera.transform;
            Vector3 forward = cameraTransform.forward;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            Vector3 position = cameraTransform.position
                + forward * Mathf.Max(0.01f, headDistance)
                + cameraTransform.right * headOffset.x
                + cameraTransform.up * headOffset.y
                + forward * headOffset.z;

            Quaternion rotation = GetCameraFacingRotation(camera, position, cameraTransform.rotation);
            pose = new Pose(position, rotation);
            return true;
        }

        private bool TryResolveObjectAnchor(
            ModuleDocument module,
            AnchorDefinition anchor,
            out Pose pose,
            out string error)
        {
            pose = new Pose(Vector3.zero, Quaternion.identity);
            error = "";

            if (sceneBindingRegistry == null)
            {
                sceneBindingRegistry = FindFirstObjectByType<SceneBindingRegistry>();
            }

            if (sceneBindingRegistry == null)
            {
                error = "SceneBindingRegistry is missing.";
                return false;
            }

            GameObject target;
            if (!sceneBindingRegistry.TryGetObjectByModuleObjectId(
                    module,
                    anchor.targetObjectId,
                    out target,
                    out error))
            {
                return false;
            }

            Camera camera = ViewerCamera;
            Quaternion rotation = GetCameraFacingRotation(camera, target.transform.position, target.transform.rotation);
            pose = new Pose(target.transform.position, rotation);
            return true;
        }

        private static Quaternion GetCameraFacingRotation(Camera camera, Vector3 position, Quaternion fallback)
        {
            if (camera == null)
            {
                return fallback;
            }

            Vector3 awayFromCamera = position - camera.transform.position;
            if (awayFromCamera.sqrMagnitude < 0.0001f)
            {
                return fallback;
            }

            return Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
        }

        private static AnchorDefinition FindAnchor(ModuleDocument module, string anchorId)
        {
            if (module.anchors == null)
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
    }
}