using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public class SimulatorRecenterController : MonoBehaviour
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        private void Awake()
        {
            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }
        }

        private void OnGUI()
        {
            if (anchorResolver == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(Screen.width - 240, 20, 220, 90), GUI.skin.box);
            GUILayout.Label("Simulator Layout");

            if (GUILayout.Button("Recenter World"))
            {
                anchorResolver.RecenterSimulatorWorldOrigin();
            }

            GUILayout.EndArea();
        }
    }
}