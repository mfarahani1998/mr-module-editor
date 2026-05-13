using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class RuntimeControlPanel : MonoBehaviour
    {
        private ModuleRunner runner;

        public void Bind(ModuleRunner moduleRunner)
        {
            runner = moduleRunner;
        }

        private void OnGUI()
        {
            if (runner == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20, Screen.height - 180, 520, 160), GUI.skin.box);
            GUILayout.Label("<b>Runtime Controls</b>");
            GUILayout.Label("State: " + runner.State);
            GUILayout.Label("Module: " + runner.CurrentModuleTitle);
            GUILayout.Label("Step: " + runner.CurrentStepDebugText);

            if (!string.IsNullOrEmpty(runner.LastError))
            {
                GUILayout.Label("Error: " + runner.LastError);
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Play"))
            {
                runner.Play();
            }

            if (GUILayout.Button("Pause"))
            {
                runner.Pause();
            }

            if (GUILayout.Button("Resume"))
            {
                runner.Resume();
            }

            if (GUILayout.Button("Stop"))
            {
                runner.Stop();
            }

            if (GUILayout.Button("Restart"))
            {
                runner.Restart();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}