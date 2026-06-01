using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public class ManualAnchorProvider : MonoBehaviour, IAnchorProvider
    {
        [SerializeField]
        private Transform manualWorldOrigin;

        [SerializeField]
        private float defaultDistanceFromViewer = 2f;

        public string ProviderId
        {
            get { return AnchorProviderIds.Manual; }
        }

        private void Awake()
        {
            EnsureManualWorldOrigin();
        }

        [ContextMenu("Calibrate Manual World Origin From Viewer")]
        public void CalibrateFromViewer()
        {
            string message;
            if (!TryCalibrateFromViewer(out message))
            {
                Debug.LogWarning(message);
            }
        }

        public bool TryCalibrateFromViewer(out string message)
        {
            message = "";
            Transform origin = EnsureManualWorldOrigin();
            Camera camera = Camera.main;
            if (origin == null || camera == null)
            {
                message = "Cannot calibrate manual anchor because the manual origin or Main Camera is missing.";
                return false;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            flatForward.Normalize();
            origin.position = camera.transform.position + flatForward * Mathf.Max(0.1f, defaultDistanceFromViewer);
            origin.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
            message = "Manual world origin calibrated in front of the viewer.";
            return true;
        }

        public bool TryResolveAnchor(
            ModuleDocument module,
            AnchorDefinition anchor,
            AnchorResolver resolver,
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

            if (anchor.type != "world")
            {
                error = "ManualAnchorProvider only resolves world anchors in Phase 5.";
                return false;
            }

            Transform origin = EnsureManualWorldOrigin();
            if (origin == null)
            {
                error = "Manual world origin is missing.";
                return false;
            }

            pose = new Pose(origin.position, origin.rotation);
            return true;
        }

        private Transform EnsureManualWorldOrigin()
        {
            if (manualWorldOrigin == null)
            {
                manualWorldOrigin = transform;
            }

            return manualWorldOrigin;
        }
    }
}
