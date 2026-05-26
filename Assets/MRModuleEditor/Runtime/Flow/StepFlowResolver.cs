using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;

namespace MRModuleEditor.Runtime.Flow
{
    public class StepFlowResolver
    {
        public string ResolveNextStepId(
            ModuleStep step,
            RuntimeContext context,
            string defaultNextStepId)
        {
            if (step == null)
            {
                return Clean(defaultNextStepId);
            }

            string mcqBranchTarget = ResolveMcqBranchTarget(step, context);
            if (!string.IsNullOrWhiteSpace(mcqBranchTarget))
            {
                return mcqBranchTarget;
            }

            string explicitNextStepId = step.GetString("nextStepId", "");
            if (!string.IsNullOrWhiteSpace(explicitNextStepId))
            {
                return explicitNextStepId;
            }

            return Clean(defaultNextStepId);
        }

        private static string ResolveMcqBranchTarget(ModuleStep step, RuntimeContext context)
        {
            if (step == null || step.type != "mcq" || context == null || context.Results == null)
            {
                return "";
            }

            bool correct;
            if (!context.Results.TryGetStepBool(step.id, "correct", out correct))
            {
                return "";
            }

            string branchKey = correct ? "onCorrectStepId" : "onWrongStepId";
            return Clean(step.GetString(branchKey, ""));
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value;
        }
    }
}