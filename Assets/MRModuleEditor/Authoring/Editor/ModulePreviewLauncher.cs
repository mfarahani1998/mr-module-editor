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
        private const string RuntimePreviewScenePath = RuntimePreviewScenePathUtility.RuntimePreviewScenePath;
        private const string PreviewRequestedKey = "MRModuleEditor.Authoring.PreviewRequested";
        private const string PreviewModulePathKey = "MRModuleEditor.Authoring.PreviewModulePathFromAssets";
        private const string PreviewStartStepIdKey = "MRModuleEditor.Authoring.PreviewStartStepId";
        private const string PreviewPrepareStepsBeforeStartKey = "MRModuleEditor.Authoring.PreviewPrepareStepsBeforeStart";

        static ModulePreviewLauncher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void LaunchPreview(string absoluteModulePath)
        {
            LaunchPreview(absoluteModulePath, "");
        }

        public static void LaunchPreview(string absoluteModulePath, string startStepId)
        {
            LaunchPreview(absoluteModulePath, startStepId, false);
        }

        public static void LaunchPreview(string absoluteModulePath, string startStepId, bool prepareStepsBeforeStart)
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

            if (!RuntimePreviewScenePathUtility.PersistAssetsRelativeModulePathForPreviewScenes(
                    relativePathFromAssets,
                    out string sceneUpdateError))
            {
                EditorUtility.DisplayDialog(
                    "Could Not Update Preview Scene",
                    sceneUpdateError,
                    "OK");
                return;
            }            

            SessionState.SetBool(PreviewRequestedKey, true);
            SessionState.SetString(PreviewModulePathKey, relativePathFromAssets);
            SessionState.SetString(PreviewStartStepIdKey, startStepId ?? "");
            SessionState.SetBool(PreviewPrepareStepsBeforeStartKey, prepareStepsBeforeStart);

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
            string startStepId = SessionState.GetString(PreviewStartStepIdKey, "");
            bool prepareStepsBeforeStart = SessionState.GetBool(PreviewPrepareStepsBeforeStartKey, false);

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

            runner.StartStepId = startStepId;
            runner.PrepareStepsBeforeStartStep = prepareStepsBeforeStart;

            if (!runner.LoadModule())
            {
                Debug.LogError("Preview failed: " + runner.LastError);
                return;
            }

            string startMessage = string.IsNullOrWhiteSpace(startStepId)
                ? ""
                : " Starting from step id: " + startStepId + ".";
            string preparationMessage = prepareStepsBeforeStart
                ? " Previous deterministic step effects will be prepared before Play reaches the selected step."
                : "";
            Debug.Log("Preview loaded." + startMessage + preparationMessage + " Press Play in the runtime control panel to start the module.");
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