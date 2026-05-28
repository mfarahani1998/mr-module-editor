using System.IO;
using MRModuleEditor.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MRModuleEditor.Authoring.Editor
{
    [InitializeOnLoad]
    public static class ModulePreviewLauncher
    {
        private const string RuntimePreviewScenePath = "Assets/MRModuleEditor/Samples/Scenes/RuntimePreview.unity";
        private const string PreviewRequestedKey = "MRModuleEditor.Authoring.PreviewRequested";
        private const string PreviewModulePathKey = "MRModuleEditor.Authoring.PreviewModulePathFromAssets";

        static ModulePreviewLauncher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void LaunchPreview(string absoluteModulePath)
        {
            string relativePathFromAssets = ToRelativePathFromAssets(absoluteModulePath);
            if (string.IsNullOrWhiteSpace(relativePathFromAssets))
            {
                EditorUtility.DisplayDialog(
                    "Preview requires an Assets path",
                    "Save the module somewhere under this project's Assets folder before previewing.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SessionState.SetBool(PreviewRequestedKey, true);
            SessionState.SetString(PreviewModulePathKey, relativePathFromAssets);

            if (EditorSceneManager.GetActiveScene().path != RuntimePreviewScenePath)
            {
                EditorSceneManager.OpenScene(RuntimePreviewScenePath);
            }

            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            if (!SessionState.GetBool(PreviewRequestedKey, false))
            {
                return;
            }

            SessionState.SetBool(PreviewRequestedKey, false);
            string relativePathFromAssets = SessionState.GetString(PreviewModulePathKey, "");

            RuntimeModuleLoader[] loaders = Object.FindObjectsByType<RuntimeModuleLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < loaders.Length; i++)
            {
                if (loaders[i] != null)
                {
                    loaders[i].LoadMode = ModuleLoadMode.AssetsRelative;
                    loaders[i].RelativeModulePathFromAssets = relativePathFromAssets;
                }
            }

            ModuleRunner runner = Object.FindFirstObjectByType<ModuleRunner>();
            if (runner == null)
            {
                Debug.LogError("Preview failed: RuntimePreview scene does not contain a ModuleRunner.");
                return;
            }

            if (!runner.LoadModule())
            {
                Debug.LogError("Preview failed: " + runner.LastError);
                return;
            }

            Debug.Log("Preview loaded. Press Play in the runtime control panel to start the module.");
        }

        private static string ToRelativePathFromAssets(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return "";
            }

            string projectRoot = Directory.GetCurrentDirectory().Replace("\\", "/");
            string assetsRoot = Path.Combine(projectRoot, "Assets").Replace("\\", "/");
            string normalizedPath = Path.GetFullPath(absolutePath).Replace("\\", "/");

            if (!normalizedPath.StartsWith(assetsRoot + "/"))
            {
                return "";
            }

            return normalizedPath.Substring(assetsRoot.Length + 1);
        }
    }
}