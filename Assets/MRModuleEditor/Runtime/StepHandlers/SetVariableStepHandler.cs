using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Preview;
using MRModuleEditor.Runtime.Variables;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class SetVariableStepHandler : IStepHandler, IPreviewPreparationStepHandler
    {
        public string StepType
        {
            get { return "setVariable"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            string error;
            if (!TryApplyVariable(step, context, out error))
            {
                if (context.LogError != null)
                {
                    context.LogError(error);
                }
                yield break;
            }

            float durationSeconds = StepParameterReader.GetDuration(step, 0f);
            if (durationSeconds > 0f)
            {
                yield return context.WaitRespectingPause(durationSeconds);
            }
        }

        public bool PrepareForPreview(
            ModuleStep step,
            RuntimeContext context,
            PreviewPreparationContext preparationContext,
            int stepIndex)
        {
            string error;
            if (!TryApplyVariable(step, context, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            preparationContext.MarkApplied(step, stepIndex, "Applied variable value without waiting for the step duration.");
            return true;
        }

        private static bool TryApplyVariable(ModuleStep step, RuntimeContext context, out string error)
        {
            error = "";
            string key = step.GetString("variableKey", "");
            if (string.IsNullOrWhiteSpace(key))
            {
                error = "setVariable is missing variableKey.";
                return false;
            }

            RuntimeVariableStore store = context.Variables;
            if (store == null)
            {
                store = Object.FindFirstObjectByType<RuntimeVariableStore>(FindObjectsInactive.Include);
            }

            if (store == null)
            {
                error = "setVariable needs a RuntimeVariableStore in the scene.";
                return false;
            }

            string valueType = step.GetString("valueType", "String");
            if (valueType == "Float")
            {
                store.SetFloat(key, step.GetFloat("floatValue", 0f));
            }
            else if (valueType == "Int")
            {
                store.SetInt(key, step.GetInt("intValue", 0));
            }
            else if (valueType == "Bool")
            {
                store.SetBool(key, step.GetBool("boolValue", false));
            }
            else
            {
                store.SetString(key, step.GetString("stringValue", ""));
            }

            store.FlushPendingUpdates();
            return true;
        }
    }
}
