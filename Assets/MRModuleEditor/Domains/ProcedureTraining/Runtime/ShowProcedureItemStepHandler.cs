using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.ObjectState;
using MRModuleEditor.Runtime.Preview;
using MRModuleEditor.Runtime.StepHandlers;
using UnityEngine;

namespace MRModuleEditor.Domains.ProcedureTraining
{
    public class ShowProcedureItemStepHandler : IStepHandler, IPreviewPreparationStepHandler
    {
        public string StepType
        {
            get { return "showProcedureItem"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string error;
            ObjectHighlightController highlightController;
            if (!TryApplyProcedureItemState(step, context, out highlightController, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null)
                {
                    context.LogError(error);
                }
                yield break;
            }

            float duration = StepParameterReader.GetDuration(step, 1f);
            if (duration > 0f)
            {
                yield return context.WaitRespectingPause(duration);
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (step.GetBool("clearHighlightOnComplete", false) && highlightController != null)
            {
                highlightController.Clear();
            }
        }

        public bool PrepareForPreview(ModuleStep step, RuntimeContext context, PreviewPreparationContext preparationContext, int stepIndex)
        {
            string error;
            ObjectHighlightController highlightController;
            if (!TryApplyProcedureItemState(step, context, out highlightController, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            preparationContext.MarkApplied(step, stepIndex, "Applied ProcedureTraining item visibility/highlight without waiting for the step duration.");
            return true;
        }

        private static bool TryApplyProcedureItemState(
            ModuleStep step,
            RuntimeContext context,
            out ObjectHighlightController highlightController,
            out string error)
        {
            highlightController = null;
            error = "";

            string objectId = step.GetString("objectId", "object.equipment_demo");
            string itemId = step.GetString("itemId", "");
            string instruction = step.GetString("instruction", "");

            GameObject target;
            ProcedureItemMarker marker;
            if (!ProcedureTrainingResolver.TryResolveProcedureObject(context, objectId, out target, out marker, out error))
            {
                return false;
            }

            target.SetActive(true);
            marker.MarkShown(itemId);

            if (step.GetBool("highlight", true))
            {
                highlightController = ObjectHighlightController.GetOrAdd(target);
                if (highlightController != null)
                {
                    string colorHex = step.GetString("colorHex", "#FFD54F");
                    float pulseAmplitude = Mathf.Max(0f, step.GetFloat("pulseAmplitude", 0.05f));
                    float pulseSeconds = Mathf.Max(0.05f, step.GetFloat("pulseSeconds", 0.8f));
                    highlightController.Apply(colorHex, pulseAmplitude, pulseSeconds);
                }
            }

            if (context.Results != null)
            {
                context.Results.SetStepString(step.id, "objectId", objectId);
                context.Results.SetStepString(step.id, "itemId", itemId);
                context.Results.SetStepBool(step.id, "shown", true);
            }

            string variableKey = step.GetString("resultVariableKey", "");
            if (!string.IsNullOrWhiteSpace(variableKey) && context.Variables != null)
            {
                context.Variables.SetString(variableKey, itemId);
                context.Variables.FlushPendingUpdates();
            }

            ShowInstruction(step, context, instruction);
            return true;
        }

        private static void ShowInstruction(ModuleStep step, RuntimeContext context, string instruction)
        {
            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowText(step.title, instruction);
            }

            if (context.SpatialUI != null && context.SpatialUI.CanShowText)
            {
                context.SpatialUI.ShowText(context.Module, step, instruction);
            }
        }
    }
}
