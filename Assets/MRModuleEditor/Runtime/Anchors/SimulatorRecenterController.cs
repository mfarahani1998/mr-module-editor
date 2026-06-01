using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.Interaction;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public class SimulatorRecenterController : MonoBehaviour
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private RuntimeModuleLoader moduleLoader;

        [SerializeField]
        private InteractionContext interactionContext;

        [SerializeField]
        private bool showOnGuiButton = true;

        [SerializeField]
        private bool showAnchorStatus = true;

        [SerializeField]
        private bool showAnchorDetails = false;

        [SerializeField]
        private bool enableKeyboardShortcut = true;

        [SerializeField]
        private KeyCode recenterKey = KeyCode.R;

        [SerializeField]
        private string recenterSignalTargetId = "runtime.world.recenter";

        private void Awake()
        {
            EnsureReferences();
        }

        private void Update()
        {
            if (!enableKeyboardShortcut)
            {
                return;
            }

            if (Input.GetKeyDown(recenterKey))
            {
                RecenterWorld(InteractionSource.Keyboard);
            }
        }

        public void RecenterWorld()
        {
            RecenterWorld(InteractionSource.Unknown);
        }

        public void RecenterWorld(InteractionSource source)
        {
            EnsureReferences();

            if (anchorResolver == null)
            {
                Debug.LogWarning("Cannot recenter world because AnchorResolver is missing.");
                return;
            }

            string error;
            if (!anchorResolver.TryRecenterSimulatorWorldOrigin(out error))
            {
                Debug.LogWarning(error);
                return;
            }

            EmitRecenterSignal(source);
        }

        private void OnGUI()
        {
            if (!showOnGuiButton)
            {
                return;
            }

            EnsureReferences();

            Rect area = showAnchorStatus
                ? new Rect(Screen.width - 400, 20, 380, showAnchorDetails ? 360 : 165)
                : new Rect(Screen.width - 260, 20, 240, 95);

            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("Runtime Layout / Calibration");

            string shortcut = enableKeyboardShortcut ? " (" + recenterKey + ")" : "";
            if (GUILayout.Button("Recenter World" + shortcut))
            {
                RecenterWorld(InteractionSource.Unknown);
            }

            GUILayout.Label("Recenter places the simulator/world origin in front of the current camera.", GUI.skin.label);

            if (showAnchorStatus)
            {
                DrawAnchorStatus();
            }

            GUILayout.EndArea();
        }

        private void DrawAnchorStatus()
        {
            ModuleDocument module = moduleLoader == null ? null : moduleLoader.LoadedModule;
            if (module == null)
            {
                GUILayout.Space(4);
                GUILayout.Label("Anchor status: no module loaded yet.");
                return;
            }

            List<RuntimeAnchorStatus> statuses = RuntimeAnchorStatusUtility.Collect(module, anchorResolver);
            int resolved = RuntimeAnchorStatusUtility.CountResolved(statuses);
            int resolvedWithoutFallback = RuntimeAnchorStatusUtility.CountResolvedWithoutFallback(statuses);
            GUILayout.Space(4);
            GUILayout.Label("Anchors: " + resolved + " / " + statuses.Count + " resolved (" + resolvedWithoutFallback + " direct)");

            if (GUILayout.Button(showAnchorDetails ? "Hide Anchor Details" : "Show Anchor Details"))
            {
                showAnchorDetails = !showAnchorDetails;
            }

            if (!showAnchorDetails)
            {
                return;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                RuntimeAnchorStatus status = statuses[i];
                if (status == null)
                {
                    continue;
                }

                string prefix = status.resolved ? (status.usedFallback ? "FALLBACK " : "OK ") : "WARN ";
                string provider = string.IsNullOrWhiteSpace(status.provider) ? "default" : status.provider;
                string state = string.IsNullOrWhiteSpace(status.state) ? "unknown" : status.state;
                string label = prefix + status.anchorId + " [" + status.anchorType + ", " + provider + ", " + state + "]";
                if (status.resolved)
                {
                    label += " @ " + FormatVector(status.position);
                    if (status.usedFallback && !string.IsNullOrWhiteSpace(status.fallbackAnchorId))
                    {
                        label += " via " + status.fallbackAnchorId;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(status.message))
                {
                    label += " — " + status.message;
                }

                GUILayout.Label(label, GUI.skin.label);
            }
        }

        private void EmitRecenterSignal(InteractionSource source)
        {
            if (interactionContext == null)
            {
                interactionContext = FindFirstObjectByType<InteractionContext>(FindObjectsInactive.Include);
            }

            if (interactionContext == null)
            {
                return;
            }

            interactionContext.Emit(new InteractionSignal
            {
                action = InteractionAction.RecenterWorld,
                source = source,
                targetId = recenterSignalTargetId ?? "",
                intPayload = 0,
                floatPayload = 1f,
                time = Time.time
            });
        }

        private void EnsureReferences()
        {
            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            if (moduleLoader == null)
            {
                moduleLoader = FindFirstObjectByType<RuntimeModuleLoader>(FindObjectsInactive.Include);
            }

            if (interactionContext == null)
            {
                interactionContext = FindFirstObjectByType<InteractionContext>(FindObjectsInactive.Include);
            }
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" + value.x.ToString("0.00") + ", " + value.y.ToString("0.00") + ", " + value.z.ToString("0.00") + ")";
        }
    }
}
