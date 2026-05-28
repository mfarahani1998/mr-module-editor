using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Variables;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class SetVariableStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "setVariable"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            string key = step.GetString("variableKey", "");
            if (string.IsNullOrWhiteSpace(key))
            {
                if (context.LogError != null)
                {
                    context.LogError("setVariable is missing variableKey.");
                }
                yield break;
            }

            RuntimeVariableStore store = context.Variables;
            if (store == null)
            {
                store = Object.FindFirstObjectByType<RuntimeVariableStore>(FindObjectsInactive.Include);
            }

            if (store == null)
            {
                if (context.LogError != null)
                {
                    context.LogError("setVariable needs a RuntimeVariableStore in the scene.");
                }
                yield break;
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

            float durationSeconds = StepParameterReader.GetDuration(step, 0f);
            if (durationSeconds > 0f)
            {
                yield return context.WaitRespectingPause(durationSeconds);
            }
        }
    }
}
