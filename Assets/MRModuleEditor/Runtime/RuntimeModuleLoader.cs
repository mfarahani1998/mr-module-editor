using System;
using System.Collections.Generic;
using System.IO;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using MRModuleEditor.Core.Validation;
using UnityEngine;

namespace MRModuleEditor.Runtime
{
    public class RuntimeModuleLoader : MonoBehaviour
    {
        [SerializeField]
        private string relativeModulePathFromAssets = ModuleFilePaths.SampleModuleRelativePathFromAssets;

        [SerializeField]
        private bool loadOnStart = true;

        private List<ValidationIssue> lastIssues = new List<ValidationIssue>();

        public ModuleDocument LoadedModule { get; private set; }

        public IReadOnlyList<ValidationIssue> LastIssues
        {
            get { return lastIssues; }
        }

        public string RelativeModulePathFromAssets
        {
            get { return relativeModulePathFromAssets; }
            set { relativeModulePathFromAssets = value; }
        }

        private void Start()
        {
            if (loadOnStart)
            {
                LoadAndValidate();
            }
        }

        [ContextMenu("Load And Validate Module")]
        public bool LoadAndValidate()
        {
            try
            {
                string absolutePath = ModuleFilePaths.FromAssetsDirectory(
                    Application.dataPath,
                    relativeModulePathFromAssets);

                LoadedModule = ModuleJsonSerializer.LoadFromFile(absolutePath);
                lastIssues = ModuleValidator.Validate(LoadedModule);

                Debug.Log("Loaded module: " + LoadedModule.title + " from " + absolutePath);
                LogIssues(lastIssues);

                bool hasErrors = ModuleValidator.HasError(lastIssues);
                if (!hasErrors)
                {
                    Debug.Log("Module validation passed with " + lastIssues.Count + " issue(s).");
                }

                return !hasErrors;
            }
            catch (Exception ex)
            {
                LoadedModule = null;
                lastIssues = new List<ValidationIssue>
                {
                    new ValidationIssue(
                        ValidationSeverity.Error,
                        "runtime.loadException",
                        ex.Message,
                        relativeModulePathFromAssets)
                };

                Debug.LogError("Failed to load module: " + ex);
                return false;
            }
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