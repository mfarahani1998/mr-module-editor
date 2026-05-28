using System;
using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Interaction;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class WaitForSignalStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "waitForSignal"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            InteractionContext interaction = context.Interaction;
            if (interaction == null)
            {
                interaction = UnityEngine.Object.FindFirstObjectByType<InteractionContext>(FindObjectsInactive.Include);
            }

            float timeoutSeconds = Mathf.Max(0f, step.GetFloat("timeoutSeconds", 0f));
            if (interaction == null)
            {
                if (context.LogError != null)
                {
                    context.LogError("waitForSignal needs an InteractionContext in the scene.");
                }

                if (timeoutSeconds > 0f)
                {
                    yield return context.WaitRespectingPause(timeoutSeconds);
                }

                yield break;
            }

            string actionFilter = step.GetString("action", "Select");
            string targetIdFilter = step.GetString("targetId", "");
            int intPayloadFilter = step.GetInt("intPayload", -1);
            string variableKey = step.GetString("variableKey", "");

            bool received = false;
            InteractionSignal receivedSignal = default(InteractionSignal);

            Action<InteractionSignal> handler = signal =>
            {
                if (received)
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

                received = true;
                receivedSignal = signal;
            };

            interaction.SignalEmitted += handler;

            float elapsed = 0f;
            while (!received)
            {
                if (context.IsCancellationRequested)
                {
                    break;
                }

                if (context.IsPaused != null && context.IsPaused())
                {
                    yield return null;
                    continue;
                }

                if (timeoutSeconds > 0f)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= timeoutSeconds)
                    {
                        break;
                    }
                }

                yield return null;
            }

            interaction.SignalEmitted -= handler;

            if (context.Results != null)
            {
                context.Results.SetStepBool(step.id, "received", received);
                context.Results.SetStepString(step.id, "targetId", received ? receivedSignal.targetId : "");
                context.Results.SetStepInt(step.id, "intPayload", received ? receivedSignal.intPayload : -1);
            }

            if (!string.IsNullOrWhiteSpace(variableKey) && context.Variables != null)
            {
                context.Variables.SetBool(variableKey, received);
                context.Variables.FlushPendingUpdates();
            }
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
