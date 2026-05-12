using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class PhaseAEditorMenu
    {
        [MenuItem("MR Module Editor/Phase A/Print Boundary Smoke Test")]
        public static void PrintBoundarySmokeTest()
        {
            Debug.Log("Phase A editor code is isolated under Authoring/Editor.");
        }
    }
}