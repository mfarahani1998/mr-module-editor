using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public class SimulatorRecenterController : MonoBehaviour
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private bool showOnGuiButton = true;

        [SerializeField]
        private bool enableKeyboardShortcut = true;

        [SerializeField]
        private KeyCode recenterKey = KeyCode.R;

        private void Awake()
        {
            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }
        }

        private void Update()
        {
            if (!enableKeyboardShortcut)
            {
                return;
            }

            if (Input.GetKeyDown(recenterKey))
            {
                RecenterWorld();
            }
        }

        public void RecenterWorld()
        {
            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            if (anchorResolver == null)
            {
                Debug.LogWarning("Cannot recenter world because AnchorResolver is missing.");
                return;
            }

            anchorResolver.RecenterSimulatorWorldOrigin();
        }

        private void OnGUI()
        {
            if (!showOnGuiButton)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(Screen.width - 240, 20, 220, 90), GUI.skin.box);
            GUILayout.Label("Runtime Layout");

            if (GUILayout.Button("Recenter World"))
            {
                RecenterWorld();
            }

            GUILayout.EndArea();
        }
    }
}