using UnityEngine;
using MRModuleEditor.Runtime.Anchors;

namespace MRModuleEditor.Runtime.UI
{
    public class RuntimeControlPanel : MonoBehaviour
    {
        [SerializeField]
        private bool showDebugOverlay = true;

        [SerializeField]
        private SimulatorRecenterController recenterController;

        private ModuleRunner runner;

        public bool ShowDebugOverlay
        {
            get { return showDebugOverlay; }
            set { showDebugOverlay = value; }
        }

        public void Bind(ModuleRunner moduleRunner)
        {
            runner = moduleRunner;
        }

        private void OnGUI()
        {
            if (!showDebugOverlay || runner == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20, Screen.height - 220, 560, 200), GUI.skin.box);
            GUILayout.Label("<b>Runtime Controls</b>");
            GUILayout.Label("State: " + runner.State);
            GUILayout.Label("Module: " + runner.CurrentModuleTitle);
            GUILayout.Label("Step: " + runner.CurrentStepDebugText);

            if (!string.IsNullOrEmpty(runner.LastError))
            {
                GUILayout.Label("Error: " + runner.LastError);
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Play")) runner.Play();
            if (GUILayout.Button("Pause")) runner.Pause();
            if (GUILayout.Button("Resume")) runner.Resume();
            if (GUILayout.Button("Stop")) runner.Stop();
            if (GUILayout.Button("Restart")) runner.Restart();
            if (GUILayout.Button("Recenter") && recenterController != null)
            {
                recenterController.RecenterWorld();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Awake()
        {
            if (recenterController == null)
            {
                recenterController = FindFirstObjectByType<SimulatorRecenterController>();
            }
        }
    }
}