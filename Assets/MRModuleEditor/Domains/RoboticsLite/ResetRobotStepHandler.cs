using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.StepHandlers;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class ResetRobotStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "resetRobot"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string objectId = step.GetString("objectId", "object.robot_preview");
            float duration = StepParameterReader.GetDuration(step, 0f);

            RobotLiteRig rig;
            string error;
            if (!RobotLiteRigResolver.TryResolveRig(context, objectId, out rig, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null)
                {
                    context.LogError(error);
                }
                yield break;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            rig.ResetRig();

            if (context.LogInfo != null)
            {
                context.LogInfo("Reset RobotLiteRig for object '" + objectId + "'.");
            }

            if (duration > 0f)
            {
                yield return context.WaitRespectingPause(duration);
            }
        }
    }
}