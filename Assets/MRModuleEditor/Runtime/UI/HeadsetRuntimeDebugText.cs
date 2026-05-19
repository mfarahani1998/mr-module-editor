using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace MRModuleEditor.Runtime.UI
{
    [DefaultExecutionOrder(-200)]
    public sealed class HeadsetRuntimeDebugText : MonoBehaviour
    {
        [SerializeField] private ModuleRunner runner;
        [SerializeField] private RuntimeModuleLoader loader;
        [SerializeField] private Camera viewerCamera;

        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, -0.45f, 1.25f);
        [SerializeField] private float refreshSeconds = 0.15f;
        [SerializeField] private int maxLogLines = 8;

        private readonly Queue<string> recentLogs = new Queue<string>();
        private Transform visualRoot;
        private TextMesh textMesh;
        private float nextRefreshTime;

        private void Awake()
        {
            if (runner == null)
            {
                runner = FindFirstObjectByType<ModuleRunner>(FindObjectsInactive.Include);
            }

            if (loader == null)
            {
                loader = FindFirstObjectByType<RuntimeModuleLoader>(FindObjectsInactive.Include);
            }

            if (viewerCamera == null)
            {
                viewerCamera = Camera.main;
            }

            visualRoot = new GameObject("HeadsetRuntimeDebugText Visual").transform;
            visualRoot.SetParent(transform, false);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(visualRoot, false);

            textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.UpperCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.022f;
            textMesh.color = Color.white;
            textMesh.text = "MR debug overlay booting...";

            Renderer renderer = textObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            Application.logMessageReceived += OnLog;
            AddLog("LOG: HeadsetRuntimeDebugText Awake");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void LateUpdate()
        {
            if (viewerCamera == null)
            {
                viewerCamera = Camera.main;
            }

            if (viewerCamera != null && visualRoot != null)
            {
                visualRoot.position = viewerCamera.transform.TransformPoint(cameraLocalPosition);
                visualRoot.rotation = viewerCamera.transform.rotation;
            }

            if (Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + refreshSeconds;
                RefreshText();
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            string prefix =
                type == LogType.Error || type == LogType.Exception ? "ERR" :
                type == LogType.Warning ? "WRN" :
                "LOG";

            AddLog(prefix + ": " + condition);
        }

        private void AddLog(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            if (line.Length > 130)
            {
                line = line.Substring(0, 130) + "...";
            }

            recentLogs.Enqueue(line);

            while (recentLogs.Count > maxLogLines)
            {
                recentLogs.Dequeue();
            }
        }

        private void RefreshText()
        {
            if (textMesh == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder(1024);
            builder.AppendLine("MR MODULE DEBUG");

            if (runner == null)
            {
                builder.AppendLine("Runner: missing");
            }
            else
            {
                builder.AppendLine("Runner: " + runner.State);
                builder.AppendLine("Module: " + runner.CurrentModuleTitle);
                builder.AppendLine("Step: " + runner.CurrentStepDebugText);

                if (!string.IsNullOrEmpty(runner.LastError))
                {
                    builder.AppendLine("Runner error: " + Trim(runner.LastError, 90));
                }
            }

            if (loader == null)
            {
                builder.AppendLine("Loader: missing");
            }
            else
            {
                builder.AppendLine(
                    "Loader: " + loader.LoadMode +
                    " loading=" + loader.IsLoading +
                    " ok=" + loader.LastLoadSucceeded);

                builder.AppendLine("Path: " + Trim(loader.LastLoadedPathOrUrl, 90));

                int issueCount = loader.LastIssues == null ? 0 : loader.LastIssues.Count;
                builder.AppendLine("Issues: " + issueCount);
            }

            builder.AppendLine("Logs:");
            foreach (string log in recentLogs)
            {
                builder.AppendLine(Trim(log, 100));
            }

            textMesh.text = builder.ToString();
        }

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value.Length <= max ? value : value.Substring(0, max) + "...";
        }
    }
}