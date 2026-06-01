using System.Collections.Generic;
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

        [Header("Providers")]
        [SerializeField]
        private MonoBehaviour[] anchorProviderBehaviours;

        [Header("Head Anchor")]
        [SerializeField]
        private float headDistance = 2.0f;

        // For a subtitle-style panel, keep this centered and slightly below the user's view.
        // X = right, Y = up, Z = extra forward, in viewer-camera space.
        [SerializeField]
        private Vector3 headOffset = new Vector3(0f, 0f, 0f);

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
            string error;
            if (!TryRecenterSimulatorWorldOrigin(out error))
            {
                Debug.LogWarning(error);
            }
        }

        public bool TryRecenterSimulatorWorldOrigin(out string error)
        {
            error = "";

            Camera camera = ViewerCamera;
            if (camera == null || simulatorWorldOrigin == null)
            {
                error = "Cannot recenter simulator origin because the camera or origin is missing.";
                return false;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            flatForward.Normalize();
            simulatorWorldOrigin.position = camera.transform.position + flatForward * defaultWorldDistance;
            simulatorWorldOrigin.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
            return true;
        }

        public bool TryResolveAnchor(
            ModuleDocument module,
            string anchorId,
            out Pose pose,
            out string error)
        {
            AnchorResolveResult result;
            bool resolved = TryResolveAnchorWithStatus(module, anchorId, out result);
            pose = result == null ? new Pose(Vector3.zero, Quaternion.identity) : result.pose;
            error = result == null ? "Anchor resolution failed." : result.message;
            return resolved;
        }

        public bool TryResolveAnchorWithStatus(
            ModuleDocument module,
            string anchorId,
            out AnchorResolveResult result)
        {
            HashSet<string> visitedAnchorIds = new HashSet<string>();
            return TryResolveAnchorWithStatus(module, anchorId, visitedAnchorIds, out result);
        }

        private bool TryResolveAnchorWithStatus(
            ModuleDocument module,
            string anchorId,
            HashSet<string> visitedAnchorIds,
            out AnchorResolveResult result)
        {
            result = AnchorResolveResult.Failed(anchorId, "Anchor resolution failed.");

            if (module == null)
            {
                result = AnchorResolveResult.Failed(anchorId, "ModuleDocument is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = "anchor.head.default";
            }

            if (visitedAnchorIds == null)
            {
                visitedAnchorIds = new HashSet<string>();
            }

            if (visitedAnchorIds.Contains(anchorId))
            {
                result = AnchorResolveResult.Failed(anchorId, "Anchor fallback cycle detected at '" + anchorId + "'.");
                return false;
            }

            visitedAnchorIds.Add(anchorId);

            AnchorDefinition anchor = FindAnchor(module, anchorId);
            if (anchor == null)
            {
                result = AnchorResolveResult.Failed(anchorId, "No anchor with id '" + anchorId + "' exists in the module.");
                return false;
            }

            Pose pose;
            string error;
            if (TryResolveWithoutFallback(module, anchor, out pose, out error))
            {
                result = new AnchorResolveResult
                {
                    requestedAnchorId = anchorId,
                    effectiveAnchorId = anchorId,
                    provider = AnchorProviderIds.Normalize(anchor.provider, anchor.type),
                    state = ResolveSuccessfulState(anchor),
                    resolved = true,
                    usedFallback = false,
                    pose = pose,
                    message = "Resolved"
                };
                return true;
            }

            if (!string.IsNullOrWhiteSpace(anchor.fallbackAnchorId))
            {
                AnchorResolveResult fallbackResult;
                if (TryResolveAnchorWithStatus(module, anchor.fallbackAnchorId, visitedAnchorIds, out fallbackResult))
                {
                    fallbackResult.requestedAnchorId = anchorId;
                    fallbackResult.usedFallback = true;
                    fallbackResult.state = AnchorCalibrationStatuses.Approximate;
                    fallbackResult.message = "Resolved via fallback anchor '" + anchor.fallbackAnchorId + "' after: " + error;
                    result = fallbackResult;
                    return true;
                }
            }

            result = AnchorResolveResult.Failed(anchorId, error);
            result.effectiveAnchorId = anchorId;
            result.provider = AnchorProviderIds.Normalize(anchor.provider, anchor.type);
            return false;
        }

        private bool TryResolveWithoutFallback(
            ModuleDocument module,
            AnchorDefinition anchor,
            out Pose pose,
            out string error)
        {
            pose = new Pose(Vector3.zero, Quaternion.identity);
            error = "";

            if (anchor == null)
            {
                error = "Anchor is null.";
                return false;
            }

            string provider = AnchorProviderIds.Normalize(anchor.provider, anchor.type);
            if (provider != AnchorProviderIds.Simulator)
            {
                IAnchorProvider customProvider = FindProvider(provider);
                if (customProvider == null)
                {
                    error = "No runtime anchor provider with id '" + provider + "' was found for anchor '" + anchor.id + "'.";
                    return false;
                }

                return customProvider.TryResolveAnchor(module, anchor, this, out pose, out error);
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

            error = "Unsupported anchor type '" + anchor.type + "' for anchor '" + anchor.id + "'.";
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

        public static Quaternion GetCameraFacingRotation(Camera camera, Vector3 position, Quaternion fallback)
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

        private IAnchorProvider FindProvider(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                return null;
            }

            if (anchorProviderBehaviours != null)
            {
                for (int i = 0; i < anchorProviderBehaviours.Length; i++)
                {
                    IAnchorProvider provider = anchorProviderBehaviours[i] as IAnchorProvider;
                    if (provider != null && provider.ProviderId == providerId)
                    {
                        return provider;
                    }
                }
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                IAnchorProvider provider = behaviours[i] as IAnchorProvider;
                if (provider != null && provider.ProviderId == providerId)
                {
                    return provider;
                }
            }

            return null;
        }

        private static string ResolveSuccessfulState(AnchorDefinition anchor)
        {
            if (anchor == null || string.IsNullOrWhiteSpace(anchor.calibrationStatus))
            {
                return AnchorCalibrationStatuses.Ready;
            }

            string authoredStatus = AnchorCalibrationStatuses.Normalize(anchor.calibrationStatus);
            if (authoredStatus == AnchorCalibrationStatuses.Lost)
            {
                return AnchorCalibrationStatuses.Approximate;
            }

            return authoredStatus;
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
