using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.Preview
{
    public static class PreviewPreparationUtility
    {
        public static string DescribeSkippedStep(ModuleStep step)
        {
            string type = step == null ? "" : step.type ?? "";
            if (type == "text")
            {
                return "Text is time/UI presentation, so preparation does not wait for or replay it.";
            }

            if (type == "image")
            {
                return "Image display is time/media presentation, so preparation does not load or replay it.";
            }

            if (type == "audio")
            {
                return "Audio playback is media/time based, so preparation does not play or fast-forward audio.";
            }

            if (type == "wait")
            {
                return "Wait contributes elapsed time only, so preparation skips the delay.";
            }

            if (type == "confirm")
            {
                return "Confirmation and interaction-signal outcomes are learner-dependent; preparation cannot honestly choose a result.";
            }

            if (type == "mcq")
            {
                return "MCQ answers and correct/wrong branches are learner-dependent; preparation cannot honestly choose a branch.";
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                return "This step has no type, so preparation cannot apply a deterministic end state.";
            }

            return "No deterministic preview-preparation handler is registered for step type '" + type + "'.";
        }

        public static bool HasFlowCaveat(ModuleStep step)
        {
            if (step == null)
            {
                return false;
            }

            return HasNonEmptyString(step, "nextStepId")
                || HasNonEmptyString(step, "onCorrectStepId")
                || HasNonEmptyString(step, "onWrongStepId");
        }

        public static bool IsLearnerDependentStep(ModuleStep step)
        {
            if (step == null)
            {
                return false;
            }

            if (step.type == "confirm" || step.type == "mcq")
            {
                return true;
            }

            return false;
        }

        private static bool HasNonEmptyString(ModuleStep step, string key)
        {
            return step != null && !string.IsNullOrWhiteSpace(step.GetString(key, ""));
        }
    }
}
