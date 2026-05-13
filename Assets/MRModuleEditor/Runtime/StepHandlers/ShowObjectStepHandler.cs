using System.Collections;
using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class ShowObjectStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "showObject"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            string objectId = step.GetString("objectId", "");
            bool visible = step.GetBool("visible", true);

            GameObject target;
            string error;
            if (!context.TryResolveObject(objectId, out target, out error))
            {
                if (context.LogError != null) context.LogError(error);
                yield break;
            }

            target.SetActive(visible);

            if (context.LogInfo != null)
            {
                context.LogInfo("Set object '" + objectId + "' visible=" + visible);
            }

            yield return null;
        }
    }
}