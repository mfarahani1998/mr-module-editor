using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.StepHandlers;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class ShowFrameStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "showFrame"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string objectId = step.GetString("objectId", "object.robot_preview");
            int jointIndex = step.GetInt("jointIndex", 0);
            bool visible = step.GetBool("visible", true);
            float duration = StepParameterReader.GetDuration(step, 0.5f);

            RobotLiteRig rig;
            string error;
            if (!TryResolveRig(context, objectId, out rig, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null) context.LogError(error);
                yield break;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (!rig.TrySetFrameVisible(jointIndex, visible, out error))
            {
                if (context.LogError != null) context.LogError(error);
                yield break;
            }

            if (duration > 0f)
            {
                yield return context.WaitRespectingPause(duration);
            }
        }

        private static bool TryResolveRig(
            RuntimeContext context,
            string objectId,
            out RobotLiteRig rig,
            out string error)
        {
            rig = null;
            error = "";

            GameObject robotObject;
            if (!context.TryResolveObject(objectId, out robotObject, out error))
            {
                return false;
            }

            rig = robotObject.GetComponentInChildren<RobotLiteRig>(true);
            if (rig == null)
            {
                error = "Object '" + objectId + "' does not have a RobotLiteRig component.";
                return false;
            }

            return true;
        }
    }
}
