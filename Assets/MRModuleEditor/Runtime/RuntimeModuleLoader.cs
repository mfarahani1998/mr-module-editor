using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using MRModuleEditor.Core.Validation;
using MRModuleEditor.Runtime.IO;
using UnityEngine;

namespace MRModuleEditor.Runtime
{
    public enum ModuleLoadMode
    {
        AssetsRelative,
        StreamingAssetsRelative,
        AbsolutePath
    }

    public class RuntimeModuleLoader : MonoBehaviour
    {
        [SerializeField]
        private ModuleLoadMode loadMode = ModuleLoadMode.AssetsRelative;

        [SerializeField]
        private string relativeModulePathFromAssets = ModuleFilePaths.SampleModuleRelativePathFromAssets;

        [SerializeField]
        private string relativeModulePathFromStreamingAssets =
            "MRModuleEditor/SampleModules/ForwardKinematicsMini/module.json";

        [SerializeField]
        private string absoluteModulePath = "";

        [SerializeField]
        private bool loadOnStart = false;

        private List<ValidationIssue> lastIssues = new List<ValidationIssue>();

        public ModuleDocument LoadedModule { get; private set; }
        public string LastLoadedAbsolutePath { get { return LastLoadedPathOrUrl; } }
        public string LastLoadedPathOrUrl { get; private set; } = "";
        public string LastLoadedDirectory { get; private set; } = "";
        public bool IsLoading { get; private set; }
        public bool LastLoadSucceeded { get; private set; }

        public IReadOnlyList<ValidationIssue> LastIssues
        {
            get { return lastIssues; }
        }

        public ModuleLoadMode LoadMode
        {
            get { return loadMode; }
            set { loadMode = value; }
        }

        public string RelativeModulePathFromAssets
        {
            get { return relativeModulePathFromAssets; }
            set { relativeModulePathFromAssets = value; }
        }

        public string RelativeModulePathFromStreamingAssets
        {
            get { return relativeModulePathFromStreamingAssets; }
            set { relativeModulePathFromStreamingAssets = value; }
        }

        public string AbsoluteModulePath
        {
            get { return absoluteModulePath; }
            set { absoluteModulePath = value; }
        }

        private void Start()
        {
            if (loadOnStart)
            {
                StartCoroutine(LoadAndValidateAsync(null));
            }
        }

        [ContextMenu("Load And Validate Module")]
        private void ContextMenuLoadAndValidate()
        {
            StartCoroutine(LoadAndValidateAsync(null));
        }

        public bool LoadAndValidate()
        {
            if (IsLoading)
            {
                SetLoadFailure("Module is already loading.", BuildModulePathOrUrlSafely());
                return false;
            }

            string modulePathOrUrl = BuildModulePathOrUrlSafely();
            if (RuntimePathUtility.RequiresUnityWebRequest(modulePathOrUrl))
            {
                SetLoadFailure(
                    "This module path must be loaded asynchronously with LoadAndValidateAsync: "
                    + modulePathOrUrl,
                    modulePathOrUrl);
                return false;
            }

            try
            {
                if (!File.Exists(modulePathOrUrl))
                {
                    SetLoadFailure("Module JSON file not found: " + modulePathOrUrl, modulePathOrUrl);
                    return false;
                }

                string json = File.ReadAllText(modulePathOrUrl);
                return FinishLoadFromJson(json, modulePathOrUrl);
            }
            catch (Exception ex)
            {
                SetLoadFailure(ex.Message, modulePathOrUrl);
                Debug.LogError("Failed to load module: " + ex);
                return false;
            }
        }

        public IEnumerator LoadAndValidateAsync(Action<bool> onFinished)
        {
            if (IsLoading)
            {
                onFinished?.Invoke(false);
                yield break;
            }

            IsLoading = true;
            LastLoadSucceeded = false;
            LoadedModule = null;
            lastIssues = new List<ValidationIssue>();

            string modulePathOrUrl = BuildModulePathOrUrlSafely();
            string json = null;
            string error = null;

            yield return RuntimeFileReader.ReadText(
                modulePathOrUrl,
                loadedText => json = loadedText,
                loadError => error = loadError);

            IsLoading = false;

            if (!string.IsNullOrEmpty(error))
            {
                SetLoadFailure(error, modulePathOrUrl);
                onFinished?.Invoke(false);
                yield break;
            }

            bool ok = FinishLoadFromJson(json, modulePathOrUrl);
            onFinished?.Invoke(ok);
        }

        private bool FinishLoadFromJson(string json, string modulePathOrUrl)
        {
            try
            {
                LoadedModule = ModuleJsonSerializer.Deserialize(json);
                lastIssues = ModuleValidator.Validate(LoadedModule);

                LastLoadedPathOrUrl = modulePathOrUrl;
                LastLoadedDirectory = RuntimePathUtility.GetDirectoryPathOrUrl(modulePathOrUrl);
                LastLoadSucceeded = !ModuleValidator.HasError(lastIssues);

                Debug.Log("Loaded module: " + LoadedModule.title + " from " + modulePathOrUrl);
                LogIssues(lastIssues);

                if (LastLoadSucceeded)
                {
                    Debug.Log("Module validation passed with " + lastIssues.Count + " issue(s).");
                }

                return LastLoadSucceeded;
            }
            catch (Exception ex)
            {
                SetLoadFailure(ex.Message, modulePathOrUrl);
                Debug.LogError("Failed to parse module JSON: " + ex);
                return false;
            }
        }

        private string BuildModulePathOrUrlSafely()
        {
            try
            {
                return BuildModulePathOrUrl();
            }
            catch (Exception ex)
            {
                Debug.LogError("Could not build module path: " + ex.Message);
                return "";
            }
        }

        private string BuildModulePathOrUrl()
        {
            if (loadMode == ModuleLoadMode.AssetsRelative)
            {
                return Path.Combine(Application.dataPath, relativeModulePathFromAssets)
                    .Replace("\\", "/");
            }

            if (loadMode == ModuleLoadMode.StreamingAssetsRelative)
            {
                return RuntimePathUtility.CombinePathOrUrl(
                    Application.streamingAssetsPath,
                    relativeModulePathFromStreamingAssets);
            }

            return absoluteModulePath.Replace("\\", "/");
        }

        private void SetLoadFailure(string message, string pathOrUrl)
        {
            LastLoadSucceeded = false;
            LoadedModule = null;
            LastLoadedPathOrUrl = pathOrUrl ?? "";
            LastLoadedDirectory = "";
            lastIssues = new List<ValidationIssue>
            {
                new ValidationIssue(
                    ValidationSeverity.Error,
                    "runtime.loadException",
                    message ?? "Unknown module load failure.",
                    pathOrUrl ?? "")
            };

            LogIssues(lastIssues);
        }

        private static void LogIssues(List<ValidationIssue> issues)
        {
            if (issues == null)
            {
                return;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                ValidationIssue issue = issues[i];
                if (issue.severity == ValidationSeverity.Error)
                {
                    Debug.LogError(issue.ToString());
                }
                else if (issue.severity == ValidationSeverity.Warning)
                {
                    Debug.LogWarning(issue.ToString());
                }
                else
                {
                    Debug.Log(issue.ToString());
                }
            }
        }
    }
}