using System;
using System.Collections;
using System.Collections.Generic;
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
        private SpatialUIService spatialUIService;

        [SerializeField]
        private SpatialTextPanel spatialTextPanel;

        [SerializeField]
        private SpatialImagePanel spatialImagePanel;

        [SerializeField]
        private SpatialMCQPanel spatialMcqPanel;

        [SerializeField]
        private bool loadOnStart = true;

        [SerializeField]
        private bool playOnStart = false;

        private readonly StepHandlerRegistry handlers = new StepHandlerRegistry();
        private Coroutine runCoroutine;
        private RuntimeExecutionToken activeExecutionToken;
        private int nextExecutionId;
        private bool stopRequested;
        private bool paused;

        public RuntimeRunnerState State { get; private set; } = RuntimeRunnerState.Idle;
        public ModuleDocument CurrentModule { get; private set; }
        public int CurrentStepIndex { get; private set; } = -1;
        public string LastError { get; private set; } = "";

        /// <summary>
        /// Legacy extension point kept for existing sample/domain components.
        /// Prefer implementing IRuntimeResettable on new runtime components.
        /// </summary>
        public event Action BeforeModuleRunReset;

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

            if (spatialImagePanel == null)
            {
                spatialImagePanel = FindFirstObjectByType<SpatialImagePanel>(FindObjectsInactive.Include);
            }

            if (spatialMcqPanel == null)
            {
                spatialMcqPanel = FindFirstObjectByType<SpatialMCQPanel>(FindObjectsInactive.Include);
            }

            if (controlPanel != null)
            {
                controlPanel.Bind(this);
            }

            if (spatialUIService == null)
            {
                spatialUIService = FindFirstObjectByType<SpatialUIService>(FindObjectsInactive.Include);
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

        private void OnDisable()
        {
            CancelActiveExecution("ModuleRunner disabled.");
        }

        public void RegisterStepHandler(IStepHandler handler)
        {
            handlers.Register(handler);
        }

        public bool LoadModule()
        {
            CancelActiveExecution("Loading module.");
            LastError = "";
            CurrentStepIndex = -1;
            CurrentModule = null;
            stopRequested = false;
            paused = false;

            if (moduleLoader == null)
            {
                SetError("RuntimeModuleLoader is missing.");
                return false;
            }

            if (moduleLoader.IsLoading)
            {
                SetError("RuntimeModuleLoader is still loading. Wait for the async load to finish before starting the runner.");
                return false;
            }

            bool ok = moduleLoader.LoadedModule != null || moduleLoader.LoadAndValidate();
            if (!ok || moduleLoader.LoadedModule == null)
            {
                string issueText = moduleLoader.LastIssues == null
                    ? "Unknown load error."
                    : "Module load/validation failed.";
                SetError(issueText);
                return false;
            }

            List<ValidationIssue> issues = moduleLoader.LastIssues == null
                ? new List<ValidationIssue>()
                : new List<ValidationIssue>(moduleLoader.LastIssues);

            if (ModuleValidator.HasError(issues))
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

            if (sceneBindingRegistry != null)
            {
                sceneBindingRegistry.CaptureRuntimeBaseline();
            }

            ClearRuntimePanels();

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

            CancelActiveExecution("Starting a new run.");
            LastError = "";
            CurrentStepIndex = -1;
            stopRequested = false;
            paused = false;

            ResetRuntimeSceneForCleanRun();

            activeExecutionToken = new RuntimeExecutionToken(++nextExecutionId);
            runCoroutine = StartCoroutine(RunModule(activeExecutionToken));
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
            CancelActiveExecution("Stopped by user.");
            paused = false;
            CurrentStepIndex = -1;
            State = CurrentModule == null ? RuntimeRunnerState.Idle : RuntimeRunnerState.Loaded;
            ResetRuntimeSceneForCleanRun();
        }

        public void Restart()
        {
            Stop();
            Play();
        }

        private IEnumerator RunModule(RuntimeExecutionToken executionToken)
        {
            if (!IsActiveExecution(executionToken))
            {
                yield break;
            }

            State = RuntimeRunnerState.Playing;
            LastError = "";

            string moduleDirectory = moduleLoader == null ? "" : moduleLoader.LastLoadedDirectory;
            RuntimeContext context = new RuntimeContext(
                CurrentModule,
                moduleDirectory,
                sceneBindingRegistry,
                displayPanel,
                anchorResolver,
                spatialTextPanel,
                spatialImagePanel,
                spatialMcqPanel,
                spatialUIService,
                executionToken,
                () => IsPausedForExecution(executionToken),
                () => IsStopRequestedForExecution(executionToken),
                message => LogInfoForExecution(executionToken, message),
                message => SetErrorForExecution(executionToken, message));

            if (CurrentModule == null || CurrentModule.steps == null)
            {
                SetErrorForExecution(executionToken, "The current module has no step list.");
                yield break;
            }

            for (int i = 0; i < CurrentModule.steps.Count; i++)
            {
                if (IsStopRequestedForExecution(executionToken))
                {
                    yield break;
                }

                CurrentStepIndex = i;
                ModuleStep step = CurrentModule.steps[i];

                if (step == null)
                {
                    SetErrorForExecution(executionToken, "Step " + i + " is null.");
                    yield break;
                }

                IStepHandler handler;
                if (!handlers.TryGet(step.type, out handler))
                {
                    SetErrorForExecution(executionToken, "No handler registered for step type '" + step.type + "'.");
                    yield break;
                }

                Debug.Log("Running step " + (i + 1) + "/" + CurrentModule.steps.Count + ": " + step.type + " - " + step.title);
                yield return RunStep(executionToken, handler, step, context);

                if (State == RuntimeRunnerState.Error || IsStopRequestedForExecution(executionToken))
                {
                    yield break;
                }
            }

            if (!IsActiveExecution(executionToken))
            {
                yield break;
            }

            CurrentStepIndex = CurrentModule.steps.Count - 1;
            State = RuntimeRunnerState.Completed;
            activeExecutionToken = null;
            runCoroutine = null;

            if (displayPanel != null)
            {
                displayPanel.ShowText("Complete", "Module finished.");
            }
        }

        private IEnumerator RunStep(
            RuntimeExecutionToken executionToken,
            IStepHandler handler,
            ModuleStep step,
            RuntimeContext context)
        {
            IEnumerator routine;
            try
            {
                routine = handler.Execute(step, context);
            }
            catch (Exception exception)
            {
                SetErrorForExecution(executionToken, "Step '" + step.id + "' failed before execution: " + exception.Message);
                yield break;
            }

            if (routine == null)
            {
                yield break;
            }

            while (!IsStopRequestedForExecution(executionToken))
            {
                object current = null;
                bool hasNext = false;
                try
                {
                    hasNext = routine.MoveNext();
                    current = hasNext ? routine.Current : null;
                }
                catch (Exception exception)
                {
                    SetErrorForExecution(executionToken, "Step '" + step.id + "' failed during execution: " + exception.Message);
                    yield break;
                }

                if (!hasNext)
                {
                    yield break;
                }

                yield return current;
            }
        }

        private void CancelActiveExecution(string reason)
        {
            stopRequested = true;

            if (activeExecutionToken != null)
            {
                activeExecutionToken.Cancel(reason);
                activeExecutionToken = null;
            }

            if (runCoroutine != null)
            {
                StopCoroutine(runCoroutine);
                runCoroutine = null;
            }
        }

        private bool IsActiveExecution(RuntimeExecutionToken executionToken)
        {
            return executionToken != null
                && activeExecutionToken == executionToken
                && !executionToken.IsCancellationRequested;
        }

        private bool IsStopRequestedForExecution(RuntimeExecutionToken executionToken)
        {
            return stopRequested
                || executionToken == null
                || executionToken.IsCancellationRequested
                || activeExecutionToken != executionToken;
        }

        private bool IsPausedForExecution(RuntimeExecutionToken executionToken)
        {
            return IsActiveExecution(executionToken) && paused;
        }

        private void ResetRuntimeSceneForCleanRun()
        {
            ClearRuntimePanels();

            if (sceneBindingRegistry != null)
            {
                sceneBindingRegistry.ResetBindableObjectsToRuntimeBaseline();
            }

            if (BeforeModuleRunReset != null)
            {
                BeforeModuleRunReset.Invoke();
            }

            ResetRuntimeComponents();
        }

        private void ResetRuntimeComponents()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                IRuntimeResettable resettable = behaviour as IRuntimeResettable;
                if (resettable == null)
                {
                    continue;
                }

                try
                {
                    resettable.ResetRuntimeState();
                }
                catch (Exception exception)
                {
                    Debug.LogError("Runtime reset failed on '" + behaviour.name + "': " + exception.Message, behaviour);
                }
            }
        }

        private void ClearRuntimePanels()
        {
            if (displayPanel != null)
            {
                displayPanel.Clear();
            }

            if (spatialTextPanel != null)
            {
                spatialTextPanel.Clear();
            }

            if (spatialImagePanel != null)
            {
                spatialImagePanel.Clear();
            }

            if (spatialMcqPanel != null)
            {
                spatialMcqPanel.Clear();
            }
        }

        private void LogInfoForExecution(RuntimeExecutionToken executionToken, string message)
        {
            if (!IsActiveExecution(executionToken))
            {
                return;
            }

            Debug.Log(message);
        }

        private void SetErrorForExecution(RuntimeExecutionToken executionToken, string message)
        {
            if (!IsActiveExecution(executionToken))
            {
                return;
            }

            LastError = message ?? "Unknown error.";
            State = RuntimeRunnerState.Error;
            executionToken.Cancel("Runtime error: " + LastError);
            stopRequested = true;
            Debug.LogError(LastError);
        }

        private void SetError(string message)
        {
            LastError = message ?? "Unknown error.";
            State = RuntimeRunnerState.Error;
            Debug.LogError(LastError);
        }
    }
}
