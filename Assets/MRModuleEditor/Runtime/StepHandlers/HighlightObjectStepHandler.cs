using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.ObjectState;
using MRModuleEditor.Runtime.Preview;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class HighlightObjectStepHandler : IStepHandler, IPreviewPreparationStepHandler
    {
        public string StepType
        {
            get { return "highlightObject"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            ObjectHighlightController controller;
            string error;
            if (!TryApplyHighlightStart(step, context, out controller, out error))
            {
                if (context.LogError != null)
                {
                    context.LogError("highlightObject could not prepare object: " + error);
                }
                yield break;
            }

            if (!step.GetBool("enabled", true))
            {
                yield break;
            }

            float durationSeconds = StepParameterReader.GetDuration(step, 0f);
            bool clearOnComplete = step.GetBool("clearOnComplete", false);

            if (durationSeconds > 0f)
            {
                yield return context.WaitRespectingPause(durationSeconds);
                if (clearOnComplete && controller != null)
                {
                    controller.Clear();
                }
            }
        }

        public bool PrepareForPreview(
            ModuleStep step,
            RuntimeContext context,
            PreviewPreparationContext preparationContext,
            int stepIndex)
        {
            ObjectHighlightController controller;
            string error;
            if (!TryApplyHighlightStart(step, context, out controller, out error))
            {
                preparationContext.AddError(step, stepIndex, "highlightObject could not prepare object: " + error);
                return false;
            }

            float durationSeconds = StepParameterReader.GetDuration(step, 0f);
            bool clearOnComplete = step.GetBool("clearOnComplete", false);
            if (durationSeconds > 0f && clearOnComplete && controller != null)
            {
                controller.Clear();
                preparationContext.MarkApplied(step, stepIndex, "Applied final highlight state: cleared after its configured duration.");
                return true;
            }

            preparationContext.MarkApplied(step, stepIndex, "Applied object highlight end state without waiting for the step duration.");
            return true;
        }

        private static bool TryApplyHighlightStart(
            ModuleStep step,
            RuntimeContext context,
            out ObjectHighlightController controller,
            out string error)
        {
            controller = null;
            error = "";

            string objectId = step.GetString("objectId", "");
            bool enabled = step.GetBool("enabled", true);
            string colorHex = step.GetString("colorHex", "#42A5FF");
            float pulseAmplitude = Mathf.Max(0f, step.GetFloat("pulseAmplitude", 0.08f));
            float pulseSeconds = Mathf.Max(0.05f, step.GetFloat("pulseSeconds", 0.8f));

            GameObject target;
            if (!context.TryResolveObject(objectId, out target, out error))
            {
                return false;
            }

            controller = ObjectHighlightController.GetOrAdd(target);
            if (controller == null)
            {
                error = "ObjectHighlightController could not be created.";
                return false;
            }

            if (!enabled)
            {
                controller.Clear();
                return true;
            }

            controller.Apply(colorHex, pulseAmplitude, pulseSeconds);
            return true;
        }
    }
}
