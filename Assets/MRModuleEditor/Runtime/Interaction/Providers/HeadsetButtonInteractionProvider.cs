using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace MRModuleEditor.Runtime.Interaction.Providers
{
    /// <summary>
    /// Minimal headset-friendly selector for active InteractableTarget objects.
    ///
    /// This intentionally does not depend on XR Interaction Toolkit. It casts a ray from
    /// the main camera, highlights the active target under the reticle, and emits Select
    /// when a headset/controller button is pressed. Gaze dwell can still be used as a
    /// fallback; this provider just gives the user a faster explicit confirmation path.
    /// </summary>
    public class HeadsetButtonInteractionProvider : MonoBehaviour, IInteractionProvider
    {
        [SerializeField]
        private InteractionContext interactionContext;

        [SerializeField]
        private bool enableHeadRayButtonSelect = true;

        [SerializeField]
        private bool usePrimaryButton = true;

        [SerializeField]
        private bool useTriggerButton = true;

        [SerializeField]
        private bool allowMouseClickInEditor = true;

        [SerializeField]
        private float rayDistance = 10f;

        [SerializeField]
        private float inputArmDelaySeconds = 0.25f;

        private readonly List<InputDevice> controllerDevices = new List<InputDevice>();
        private int observedActiveVersion = -1;
        private float armedTime;
        private bool previousButtonPressed;
        private InteractableTarget currentTarget;

        public InteractionSource Source
        {
            get { return InteractionSource.ControllerRay; }
        }

        public bool ProviderEnabled
        {
            get { return isActiveAndEnabled && enableHeadRayButtonSelect; }
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
            rayDistance = Mathf.Max(0.1f, rayDistance);
            inputArmDelaySeconds = Mathf.Max(0f, inputArmDelaySeconds);
        }

        private void Update()
        {
            InteractionContext context = Context;
            if (context == null)
            {
                ClearCurrentTarget(false);
                previousButtonPressed = false;
                return;
            }

            if (observedActiveVersion != context.ActiveVersion)
            {
                observedActiveVersion = context.ActiveVersion;
                armedTime = Time.time + inputArmDelaySeconds;
                ClearCurrentTarget(false);
                previousButtonPressed = false;
            }

            if (!ProviderEnabled || context.ActiveTargetCount == 0 || Time.time < armedTime)
            {
                ClearCurrentTarget(true);
                previousButtonPressed = IsSelectButtonPressed();
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                ClearCurrentTarget(true);
                previousButtonPressed = IsSelectButtonPressed();
                return;
            }

            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            InteractableTarget hitTarget = FindClosestActiveTarget(context, ray);
            if (hitTarget != currentTarget)
            {
                ClearCurrentTarget(true);
                currentTarget = hitTarget;
                if (currentTarget != null)
                {
                    context.EmitHoverEnter(currentTarget, Source);
                    context.EmitHoverProgress(currentTarget, Source, 1f);
                }
            }

            bool buttonPressed = IsSelectButtonPressed();
            bool buttonDown = buttonPressed && !previousButtonPressed;
            previousButtonPressed = buttonPressed;

            if (buttonDown && currentTarget != null)
            {
                context.EmitSelect(currentTarget, Source);
            }
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

        private bool IsSelectButtonPressed()
        {
#if UNITY_EDITOR
            if (allowMouseClickInEditor && Input.GetMouseButton(0))
            {
                return true;
            }
#endif

            bool pressed = false;
            controllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller,
                controllerDevices);

            for (int i = 0; i < controllerDevices.Count; i++)
            {
                InputDevice device = controllerDevices[i];
                if (!device.isValid)
                {
                    continue;
                }

                bool value;
                if (usePrimaryButton && device.TryGetFeatureValue(CommonUsages.primaryButton, out value) && value)
                {
                    pressed = true;
                    break;
                }

                if (useTriggerButton && device.TryGetFeatureValue(CommonUsages.triggerButton, out value) && value)
                {
                    pressed = true;
                    break;
                }
            }

            return pressed;
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
        }
    }
}
