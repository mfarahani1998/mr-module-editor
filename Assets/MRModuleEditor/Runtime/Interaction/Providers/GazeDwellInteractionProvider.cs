using UnityEngine;
using UnityEngine.XR;

namespace MRModuleEditor.Runtime.Interaction.Providers
{
    public class GazeDwellInteractionProvider : MonoBehaviour, IInteractionProvider
    {
        private enum GazeDwellMode
        {
            Disabled,
            HeadsetOnly,
            Always
        }

        [SerializeField]
        private InteractionContext interactionContext;

        [SerializeField]
        private bool enableGazeDwell = true;

        [SerializeField]
        private GazeDwellMode gazeDwellMode = GazeDwellMode.HeadsetOnly;

        [SerializeField]
        private float dwellSeconds = 1.0f;

        [SerializeField]
        private float inputArmDelaySeconds = 0.35f;

        [SerializeField]
        private bool requireFreshTarget = true;

        [SerializeField]
        private float rayDistance = 10f;

        private int observedActiveVersion = -1;
        private float armedTime;
        private bool needsFreshTarget;
        private bool selectedForCurrentTargetSet;
        private InteractableTarget currentTarget;
        private float dwellTimer;

        public InteractionSource Source
        {
            get { return InteractionSource.HeadGaze; }
        }

        public bool ProviderEnabled
        {
            get { return isActiveAndEnabled && IsGazeAvailable(); }
        }

        private InteractionContext Context
        {
            get
            {
                if (interactionContext == null)
                {
                    interactionContext = FindFirstObjectByType<InteractionContext>(FindObjectsInactive.Include);
                }

                return interactionContext;
            }
        }

        private void OnValidate()
        {
            dwellSeconds = Mathf.Max(0.05f, dwellSeconds);
            inputArmDelaySeconds = Mathf.Max(0f, inputArmDelaySeconds);
            rayDistance = Mathf.Max(0.1f, rayDistance);
        }

        private void Update()
        {
            InteractionContext context = Context;
            if (context == null)
            {
                ClearCurrentTarget(false);
                return;
            }

            if (observedActiveVersion != context.ActiveVersion)
            {
                ResetForActiveTargetSet(context.ActiveVersion);
            }

            if (!ProviderEnabled || context.ActiveTargetCount == 0 || selectedForCurrentTargetSet)
            {
                ClearCurrentTarget(true);
                return;
            }

            if (Time.time < armedTime)
            {
                ClearCurrentTarget(true);
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                ClearCurrentTarget(true);
                return;
            }

            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            InteractableTarget hitTarget = FindClosestActiveTarget(context, ray);

            if (hitTarget == null)
            {
                ClearCurrentTarget(true);
                needsFreshTarget = false;
                return;
            }

            if (needsFreshTarget)
            {
                ClearCurrentTarget(true);
                return;
            }

            if (hitTarget != currentTarget)
            {
                ClearCurrentTarget(true);
                currentTarget = hitTarget;
                dwellTimer = 0f;
                context.EmitHoverEnter(currentTarget, Source);
            }

            dwellTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(dwellTimer / Mathf.Max(0.05f, dwellSeconds));
            context.EmitHoverProgress(currentTarget, Source, progress);

            if (dwellTimer >= dwellSeconds)
            {
                context.EmitSelect(currentTarget, Source);
                selectedForCurrentTargetSet = true;
                ClearCurrentTarget(true);
            }
        }

        private void ResetForActiveTargetSet(int activeVersion)
        {
            observedActiveVersion = activeVersion;
            armedTime = Time.time + inputArmDelaySeconds;
            needsFreshTarget = requireFreshTarget;
            selectedForCurrentTargetSet = false;
            ClearCurrentTarget(false);
        }

        private void ClearCurrentTarget(bool emitHoverExit)
        {
            if (currentTarget != null && emitHoverExit)
            {
                InteractionContext context = Context;
                if (context != null)
                {
                    context.EmitHoverExit(currentTarget, Source);
                }
            }

            currentTarget = null;
            dwellTimer = 0f;
        }

        private InteractableTarget FindClosestActiveTarget(InteractionContext context, Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance);
            InteractableTarget bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null)
                {
                    continue;
                }

                InteractableTarget target = collider.GetComponentInParent<InteractableTarget>();
                if (target == null || !context.IsTargetActive(target))
                {
                    continue;
                }

                if (hits[i].distance >= bestDistance)
                {
                    continue;
                }

                bestTarget = target;
                bestDistance = hits[i].distance;
            }

            return bestTarget;
        }

        private bool IsGazeAvailable()
        {
            if (!enableGazeDwell || gazeDwellMode == GazeDwellMode.Disabled)
            {
                return false;
            }

            if (gazeDwellMode == GazeDwellMode.Always)
            {
                return true;
            }

#if UNITY_EDITOR
            return false;
#else
            return XRSettings.isDeviceActive;
#endif
        }
    }
}