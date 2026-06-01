using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.ObjectState;
using MRModuleEditor.Runtime.StepHandlers;
using UnityEngine;

namespace MRModuleEditor.Domains.ProcedureTraining
{
    public class CheckSafetyPointStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "checkSafetyPoint"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string objectId = step.GetString("objectId", "object.equipment_demo");
            string safetyPointId = step.GetString("safetyPointId", "");
            string prompt = step.GetString("prompt", "Confirm the safety point before continuing.");
            string buttonLabel = step.GetString("buttonLabel", "Mark Checked");
            string status = step.GetString("status", "Checked");

            GameObject target;
            ProcedureItemMarker marker;
            string error;
            if (!ProcedureTrainingResolver.TryResolveProcedureObject(context, objectId, out target, out marker, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null)
                {
                    context.LogError(error);
                }
                yield break;
            }

            bool hasDebugConfirm = context.DisplayPanel != null && context.DisplayPanel.ShowDebugOverlay;
            bool hasSpatialConfirm = context.SpatialUI != null && context.SpatialUI.CanShowConfirm;
            float autoCompleteAfterSeconds = Mathf.Max(0f, step.GetFloat("autoCompleteAfterSeconds", 0f));

            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowConfirm(step.title, prompt, buttonLabel);
            }

            if (context.SpatialUI != null)
            {
                context.SpatialUI.ShowConfirm(context.Module, step, prompt, buttonLabel);
            }

            if (!hasDebugConfirm && !hasSpatialConfirm && autoCompleteAfterSeconds <= 0f)
            {
                autoCompleteAfterSeconds = Mathf.Max(0.01f, StepParameterReader.GetDuration(step, 0.25f));
            }

            float elapsed = 0f;
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
                    break;
                }

                if (hasSpatialConfirm && context.SpatialUI.HasConfirmation)
                {
                    break;
                }

                if (autoCompleteAfterSeconds > 0f)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= autoCompleteAfterSeconds)
                    {
                        break;
                    }
                }

                yield return null;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            marker.MarkSafetyPoint(safetyPointId, status);
            RecordResult(step, context, objectId, safetyPointId, status);

            if (step.GetBool("highlightOnComplete", false))
            {
                ObjectHighlightController highlight = ObjectHighlightController.GetOrAdd(target);
                if (highlight != null)
                {
                    highlight.Apply(step.GetString("highlightColorHex", "#66BB6A"), 0.03f, 0.8f);
                }
            }

            if (context.SpatialUI != null)
            {
                context.SpatialUI.ClearStep(step.id);
            }
        }

        private static void RecordResult(ModuleStep step, RuntimeContext context, string objectId, string safetyPointId, string status)
        {
            if (context.Results != null)
            {
                context.Results.SetStepString(step.id, "objectId", objectId);
                context.Results.SetStepString(step.id, "safetyPointId", safetyPointId);
                context.Results.SetStepString(step.id, "status", status);
                context.Results.SetStepBool(step.id, "checked", true);
            }

            string variableKey = step.GetString("resultVariableKey", "");
            if (!string.IsNullOrWhiteSpace(variableKey) && context.Variables != null)
            {
                context.Variables.SetString(variableKey, status);
                context.Variables.FlushPendingUpdates();
            }
        }
    }
}
