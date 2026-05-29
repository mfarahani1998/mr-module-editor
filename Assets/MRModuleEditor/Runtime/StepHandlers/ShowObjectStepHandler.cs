using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Preview;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class ShowObjectStepHandler : IStepHandler, IPreviewPreparationStepHandler
    {
        public string StepType
        {
            get { return "showObject"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            string error;
            if (!TryApplyVisibility(step, context, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null) context.LogError(error);
                yield break;
            }

            if (context.LogInfo != null)
            {
                context.LogInfo("Set object '" + step.GetString("objectId", "") + "' visible=" + step.GetBool("visible", true));
            }

            yield return null;
        }

        public bool PrepareForPreview(
            ModuleStep step,
            RuntimeContext context,
            PreviewPreparationContext preparationContext,
            int stepIndex)
        {
            string error;
            if (!TryApplyVisibility(step, context, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            preparationContext.MarkApplied(step, stepIndex, "Applied object visibility state.");
            return true;
        }

        private static bool TryApplyVisibility(ModuleStep step, RuntimeContext context, out string error)
        {
            error = "";
            if (context.IsCancellationRequested)
            {
                error = "The current module execution has been cancelled.";
                return false;
            }

            string objectId = step.GetString("objectId", "");
            bool visible = step.GetBool("visible", true);

            GameObject target;
            if (!context.TryResolveObject(objectId, out target, out error))
            {
                return false;
            }

            if (context.IsCancellationRequested)
            {
                error = "The current module execution has been cancelled.";
                return false;
            }

            target.SetActive(visible);
            return true;
        }
    }
}
