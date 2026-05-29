using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.Preview;
using MRModuleEditor.Runtime.StepHandlers;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class ResetRobotStepHandler : IStepHandler, IPreviewPreparationStepHandler
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

            string error;
            if (!TryResetRig(context, objectId, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null)
                {
                    context.LogError(error);
                }
                yield break;
            }

            if (context.LogInfo != null)
            {
                context.LogInfo("Reset RobotLiteRig for object '" + objectId + "'.");
            }

            if (duration > 0f)
            {
                yield return context.WaitRespectingPause(duration);
            }
        }

        public bool PrepareForPreview(
            ModuleStep step,
            RuntimeContext context,
            PreviewPreparationContext preparationContext,
            int stepIndex)
        {
            string objectId = step.GetString("objectId", "object.robot_preview");
            string error;
            if (!TryResetRig(context, objectId, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            preparationContext.MarkApplied(step, stepIndex, "Reset robot rig without waiting for the step duration.");
            return true;
        }

        private static bool TryResetRig(RuntimeContext context, string objectId, out string error)
        {
            RobotLiteRig rig;
            if (!RobotLiteRigResolver.TryResolveRig(context, objectId, out rig, out error))
            {
                return false;
            }

            rig.ResetRig();
            return true;
        }
    }
}
