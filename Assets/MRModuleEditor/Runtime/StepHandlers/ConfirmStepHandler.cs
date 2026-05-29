using System;
using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Interaction;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class ConfirmStepHandler : IStepHandler
    {
        private const string StepIdPlaceholder = "{stepId}";

        public string StepType
        {
            get { return "confirm"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string message = step.GetString("message", "Continue when you are ready.");
            string buttonLabel = step.GetString("buttonLabel", "Continue");
            float autoContinueAfterSeconds = ReadAutoContinueAfterSeconds(step);
            bool completeOnSignal = ShouldCompleteOnSignal(step);

            SignalWaitState signalWait = SignalWaitState.None;
            if (completeOnSignal)
            {
                signalWait = SignalWaitState.Create(step, context);
            }

            bool hasDebugConfirm = context.DisplayPanel != null && context.DisplayPanel.ShowDebugOverlay;
            bool hasSpatialConfirm = context.SpatialUI != null && context.SpatialUI.CanShowConfirm;
            bool canCompleteFromSignal = signalWait.IsEnabled;

            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowConfirm(step.title, message, buttonLabel);
            }

            if (context.SpatialUI != null)
            {
                context.SpatialUI.ShowConfirm(context.Module, step, message, buttonLabel);
            }

            if (context.LogInfo != null)
            {
                context.LogInfo("Confirm step: " + step.title);
            }

            if (!hasDebugConfirm && !hasSpatialConfirm && !canCompleteFromSignal && autoContinueAfterSeconds <= 0f)
            {
                autoContinueAfterSeconds = StepParameterReader.GetDuration(step, 1f);
            }

            bool confirmed = false;
            bool timedOut = false;
            float elapsed = 0f;
            signalWait.Subscribe();

            try
            {
                while (true)
                {
                    if (context.IsCancellationRequested)
                    {
                        yield break;
                    }

                    if (context.IsPaused != null && context.IsPaused())
                    {
                        yield return null;
                        continue;
                    }

                    if (hasDebugConfirm && context.DisplayPanel.HasConfirmation)
                    {
                        confirmed = true;
                        break;
                    }

                    if (hasSpatialConfirm && context.SpatialUI.HasConfirmation)
                    {
                        confirmed = true;
                        break;
                    }

                    if (signalWait.Received)
                    {
                        break;
                    }

                    if (autoContinueAfterSeconds > 0f)
                    {
                        elapsed += Time.deltaTime;
                        if (elapsed >= autoContinueAfterSeconds)
                        {
                            timedOut = true;
                            break;
                        }
                    }

                    yield return null;
                }
            }
            finally
            {
                signalWait.Unsubscribe();
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            RecordResult(step, context, confirmed, timedOut, signalWait);

            if (context.SpatialUI != null)
            {
                context.SpatialUI.ClearStep(step.id);
            }
        }

        private static float ReadAutoContinueAfterSeconds(ModuleStep step)
        {
            if (step.HasParameter("autoContinueAfterSeconds"))
            {
                float autoContinueAfterSeconds = Mathf.Max(0f, step.GetFloat("autoContinueAfterSeconds", 0f));
                if (autoContinueAfterSeconds > 0f || !step.HasParameter("timeoutSeconds"))
                {
                    return autoContinueAfterSeconds;
                }
            }

            // Legacy migration path: the old signal-waiting step used timeoutSeconds before it was merged
            // into confirm. New modules should use autoContinueAfterSeconds.
            if (step.HasParameter("timeoutSeconds"))
            {
                return Mathf.Max(0f, step.GetFloat("timeoutSeconds", 0f));
            }

            return 0f;
        }

        private static bool ShouldCompleteOnSignal(ModuleStep step)
        {
            if (step.HasParameter("completeOnSignal"))
            {
                return step.GetBool("completeOnSignal", false);
            }

            // Legacy migration path for modules whose type was changed from the old
            // signal-waiting step to confirm before the new catalog defaults were applied.
            return HasLegacySignalParameters(step);
        }

        private static bool HasLegacySignalParameters(ModuleStep step)
        {
            return step != null
                && (step.HasParameter("action")
                    || step.HasParameter("targetId")
                    || step.HasParameter("intPayload")
                    || step.HasParameter("timeoutSeconds")
                    || step.HasParameter("variableKey"));
        }

        private static void RecordResult(
            ModuleStep step,
            RuntimeContext context,
            bool confirmed,
            bool timedOut,
            SignalWaitState signalWait)
        {
            bool signalReceived = signalWait.Received;
            string completionReason = signalReceived
                ? "signal"
                : confirmed
                    ? "confirm"
                    : timedOut
                        ? "timeout"
                        : "unknown";

            if (context.Results != null)
            {
                context.Results.SetStepBool(step.id, "confirmed", confirmed);
                context.Results.SetStepBool(step.id, "signalReceived", signalReceived);
                context.Results.SetStepBool(step.id, "received", signalReceived);
                context.Results.SetStepString(step.id, "completionReason", completionReason);
                context.Results.SetStepString(step.id, "targetId", signalReceived ? signalWait.ReceivedSignal.targetId : "");
                context.Results.SetStepInt(step.id, "intPayload", signalReceived ? signalWait.ReceivedSignal.intPayload : -1);
            }

            string resultVariableKey = signalWait.ResultVariableKey;
            if (!string.IsNullOrWhiteSpace(resultVariableKey) && context.Variables != null)
            {
                context.Variables.SetBool(resultVariableKey, signalReceived);
                context.Variables.FlushPendingUpdates();
            }
        }

        private sealed class SignalWaitState
        {
            private readonly InteractionContext interaction;
            private readonly string actionFilter;
            private readonly string targetIdFilter;
            private readonly int intPayloadFilter;
            private readonly Action<InteractionSignal> handler;

            public static readonly SignalWaitState None = new SignalWaitState(
                null,
                "",
                "",
                -1,
                "");

            private SignalWaitState(
                InteractionContext interaction,
                string actionFilter,
                string targetIdFilter,
                int intPayloadFilter,
                string resultVariableKey)
            {
                this.interaction = interaction;
                this.actionFilter = actionFilter ?? "";
                this.targetIdFilter = targetIdFilter ?? "";
                this.intPayloadFilter = intPayloadFilter;
                ResultVariableKey = resultVariableKey ?? "";
                handler = TryReceive;
                Received = false;
                ReceivedSignal = default(InteractionSignal);
            }

            public bool IsEnabled
            {
                get { return interaction != null && handler != null; }
            }

            public bool Received { get; private set; }
            public InteractionSignal ReceivedSignal { get; private set; }
            public string ResultVariableKey { get; private set; }

            public static SignalWaitState Create(ModuleStep step, RuntimeContext context)
            {
                InteractionContext interaction = context.Interaction;
                if (interaction == null)
                {
                    interaction = UnityEngine.Object.FindFirstObjectByType<InteractionContext>(FindObjectsInactive.Include);
                }

                string actionFilter = ReadSignalAction(step);
                string targetIdFilter = ReadSignalTargetId(step);
                int intPayloadFilter = ReadSignalIntPayload(step);
                string resultVariableKey = ReadResultVariableKey(step);

                if (interaction == null && context.LogInfo != null)
                {
                    context.LogInfo("Confirm step has interaction-signal completion enabled, but no InteractionContext was found. Falling back to regular confirmation/timeout behavior.");
                }

                return new SignalWaitState(
                    interaction,
                    actionFilter,
                    targetIdFilter,
                    intPayloadFilter,
                    resultVariableKey);
            }

            public void Subscribe()
            {
                if (interaction != null && handler != null)
                {
                    interaction.SignalEmitted += handler;
                }
            }

            public void Unsubscribe()
            {
                if (interaction != null && handler != null)
                {
                    interaction.SignalEmitted -= handler;
                }
            }

            private void TryReceive(InteractionSignal signal)
            {
                if (Received)
                {
                    return;
                }

                if (!MatchesAction(actionFilter, signal.action))
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(targetIdFilter) && signal.targetId != targetIdFilter)
                {
                    return;
                }

                if (intPayloadFilter >= 0 && signal.intPayload != intPayloadFilter)
                {
                    return;
                }

                Received = true;
                ReceivedSignal = signal;
            }

            private static string ReadSignalAction(ModuleStep step)
            {
                if (step.HasParameter("signalAction"))
                {
                    return step.GetString("signalAction", "Select");
                }

                return step.GetString("action", "Select");
            }

            private static string ReadSignalTargetId(ModuleStep step)
            {
                if (step.HasParameter("signalTargetId"))
                {
                    return ExpandSignalTargetId(step, step.GetString("signalTargetId", ""));
                }

                if (HasLegacySignalParameters(step))
                {
                    // The removed signal-waiting step defaulted to an empty target filter,
                    // meaning any target. Preserve that behavior for partially migrated JSON.
                    return ExpandSignalTargetId(step, step.GetString("targetId", ""));
                }

                return BuildDefaultConfirmTargetId(step.id);
            }

            private static int ReadSignalIntPayload(ModuleStep step)
            {
                if (step.HasParameter("signalIntPayload"))
                {
                    return step.GetInt("signalIntPayload", -1);
                }

                return step.GetInt("intPayload", -1);
            }

            private static string ReadResultVariableKey(ModuleStep step)
            {
                if (step.HasParameter("resultVariableKey"))
                {
                    return step.GetString("resultVariableKey", "");
                }

                return step.GetString("variableKey", "");
            }
        }

        private static string ExpandSignalTargetId(ModuleStep step, string targetId)
        {
            if (targetId == null)
            {
                return "";
            }

            return targetId.Replace(StepIdPlaceholder, step == null ? "" : step.id ?? "");
        }

        private static string BuildDefaultConfirmTargetId(string stepId)
        {
            return (stepId ?? "") + ".confirm";
        }

        private static bool MatchesAction(string actionFilter, InteractionAction actualAction)
        {
            if (string.IsNullOrWhiteSpace(actionFilter) || actionFilter == "Any")
            {
                return true;
            }

            InteractionAction parsed;
            if (!Enum.TryParse(actionFilter, true, out parsed))
            {
                return false;
            }

            return parsed == actualAction;
        }
    }
}
