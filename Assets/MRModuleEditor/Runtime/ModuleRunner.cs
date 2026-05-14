using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Validation;
using MRModuleEditor.Runtime.Anchors;
using MRModuleEditor.Runtime.SceneBinding;
using MRModuleEditor.Runtime.StepHandlers;
using MRModuleEditor.Runtime.UI;
using UnityEngine;

namespace MRModuleEditor.Runtime
{
    public class ModuleRunner : MonoBehaviour
    {
        [SerializeField]
        private RuntimeModuleLoader moduleLoader;

        [SerializeField]
        private SceneBindingRegistry sceneBindingRegistry;

        [SerializeField]
        private RuntimeDisplayPanel displayPanel;

        [SerializeField]
        private RuntimeControlPanel controlPanel;

        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private RuntimeLayoutApplier layoutApplier;

        [SerializeField]
        private SpatialTextPanel spatialTextPanel;

        [SerializeField]
        private bool loadOnStart = true;

        [SerializeField]
        private bool playOnStart = false;

        private readonly StepHandlerRegistry handlers = new StepHandlerRegistry();
        private Coroutine runCoroutine;
        private bool stopRequested;
        private bool paused;

        public RuntimeRunnerState State { get; private set; } = RuntimeRunnerState.Idle;
        public ModuleDocument CurrentModule { get; private set; }
        public int CurrentStepIndex { get; private set; } = -1;
        public string LastError { get; private set; } = "";

        public string CurrentModuleTitle
        {
            get { return CurrentModule == null ? "None" : CurrentModule.title; }
        }

        public string CurrentStepDebugText
        {
            get
            {
                if (CurrentModule == null || CurrentModule.steps == null || CurrentModule.steps.Count == 0)
                {
                    return "None";
                }

                if (CurrentStepIndex < 0)
                {
                    return "Not started / " + CurrentModule.steps.Count;
                }

                int displayIndex = Mathf.Clamp(CurrentStepIndex + 1, 1, CurrentModule.steps.Count);
                return displayIndex + " / " + CurrentModule.steps.Count;
            }
        }

        private void Awake()
        {
            if (moduleLoader == null)
            {
                moduleLoader = GetComponent<RuntimeModuleLoader>();
            }

            if (sceneBindingRegistry == null)
            {
                sceneBindingRegistry = FindFirstObjectByType<SceneBindingRegistry>();
            }

            if (displayPanel == null)
            {
                displayPanel = FindFirstObjectByType<RuntimeDisplayPanel>();
            }

            if (controlPanel == null)
            {
                controlPanel = FindFirstObjectByType<RuntimeControlPanel>();
            }

            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            if (layoutApplier == null)
            {
                layoutApplier = FindFirstObjectByType<RuntimeLayoutApplier>();
            }

            if (spatialTextPanel == null)
            {
                spatialTextPanel = FindFirstObjectByType<SpatialTextPanel>(FindObjectsInactive.Include);
            }

            if (controlPanel != null)
            {
                controlPanel.Bind(this);
            }

            handlers.RegisterDefaultHandlers();
        }

        private void Start()
        {
            if (loadOnStart)
            {
                LoadModule();
            }

            if (playOnStart)
            {
                Play();
            }
        }

        public void RegisterStepHandler(IStepHandler handler)
        {
            handlers.Register(handler);
        }

        public bool LoadModule()
        {
            LastError = "";
            CurrentStepIndex = -1;
            stopRequested = false;
            paused = false;

            if (moduleLoader == null)
            {
                SetError("RuntimeModuleLoader is missing.");
                return false;
            }

            bool ok = moduleLoader.LoadAndValidate();
            if (!ok || moduleLoader.LoadedModule == null)
            {
                string issueText = moduleLoader.LastIssues == null ? "Unknown load error." : "Module load/validation failed.";
                SetError(issueText);
                return false;
            }

            if (ModuleValidator.HasError(new System.Collections.Generic.List<ValidationIssue>(moduleLoader.LastIssues)))
            {
                SetError("Module validation has errors.");
                return false;
            }

            CurrentModule = moduleLoader.LoadedModule;

            if (sceneBindingRegistry != null)
            {
                sceneBindingRegistry.Rebuild();
            }

            if (layoutApplier != null)
            {
                layoutApplier.ApplyObjectLayouts(CurrentModule);
            }

            if (displayPanel != null)
            {
                displayPanel.Clear();
            }

            if (spatialTextPanel != null)
            {
                spatialTextPanel.Clear();
            }

            State = RuntimeRunnerState.Loaded;
            Debug.Log("ModuleRunner loaded module: " + CurrentModule.title);
            return true;
        }

        public void Play()
        {
            if (State == RuntimeRunnerState.Playing)
            {
                return;
            }

            if (CurrentModule == null)
            {
                if (!LoadModule())
                {
                    return;
                }
            }

            if (runCoroutine != null)
            {
                StopCoroutine(runCoroutine);
            }

            stopRequested = false;
            paused = false;
            runCoroutine = StartCoroutine(RunModule());
        }

        public void Pause()
        {
            if (State == RuntimeRunnerState.Playing)
            {
                paused = true;
                State = RuntimeRunnerState.Paused;
            }
        }

        public void Resume()
        {
            if (State == RuntimeRunnerState.Paused)
            {
                paused = false;
                State = RuntimeRunnerState.Playing;
            }
        }

        public void Stop()
        {
            stopRequested = true;
            paused = false;

            if (runCoroutine != null)
            {
                StopCoroutine(runCoroutine);
                runCoroutine = null;
            }

            CurrentStepIndex = -1;
            State = CurrentModule == null ? RuntimeRunnerState.Idle : RuntimeRunnerState.Loaded;

            if (displayPanel != null)
            {
                displayPanel.Clear();
            }

            if (spatialTextPanel != null)
            {
                spatialTextPanel.Clear();
            }
        }

        public void Restart()
        {
            Stop();
            Play();
        }

        private IEnumerator RunModule()
        {
            State = RuntimeRunnerState.Playing;
            LastError = "";

            RuntimeContext context = new RuntimeContext(
                CurrentModule,
                moduleLoader.LastLoadedDirectory,
                sceneBindingRegistry,
                displayPanel,
                anchorResolver,
                spatialTextPanel,
                IsPaused,
                IsStopRequested,
                message => Debug.Log(message),
                SetError);

            for (int i = 0; i < CurrentModule.steps.Count; i++)
            {
                if (stopRequested)
                {
                    yield break;
                }

                CurrentStepIndex = i;
                ModuleStep step = CurrentModule.steps[i];

                if (step == null)
                {
                    SetError("Step " + i + " is null.");
                    yield break;
                }

                IStepHandler handler;
                if (!handlers.TryGet(step.type, out handler))
                {
                    SetError("No handler registered for step type '" + step.type + "'.");
                    yield break;
                }

                Debug.Log("Running step " + (i + 1) + "/" + CurrentModule.steps.Count + ": " + step.type + " - " + step.title);
                yield return StartCoroutine(handler.Execute(step, context));

                if (State == RuntimeRunnerState.Error)
                {
                    yield break;
                }
            }

            CurrentStepIndex = CurrentModule.steps.Count - 1;
            State = RuntimeRunnerState.Completed;

            if (displayPanel != null)
            {
                displayPanel.ShowText("Complete", "Module finished.");
            }
        }

        private bool IsPaused()
        {
            return paused;
        }

        private bool IsStopRequested()
        {
            return stopRequested;
        }

        private void SetError(string message)
        {
            LastError = message ?? "Unknown error.";
            State = RuntimeRunnerState.Error;
            Debug.LogError(LastError);
        }
    }
}