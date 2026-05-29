using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.Preview
{
    public sealed class PreviewPreparationIssue
    {
        public PreviewPreparationIssue(
            PreviewPreparationSeverity severity,
            ModuleStep step,
            int stepIndex,
            string message)
        {
            Severity = severity;
            StepId = step == null ? "" : step.id ?? "";
            StepTitle = step == null ? "" : step.title ?? "";
            StepType = step == null ? "" : step.type ?? "";
            StepIndex = stepIndex;
            Message = message ?? "";
        }

        public PreviewPreparationSeverity Severity { get; private set; }
        public string StepId { get; private set; }
        public string StepTitle { get; private set; }
        public string StepType { get; private set; }
        public int StepIndex { get; private set; }
        public string Message { get; private set; }

        public override string ToString()
        {
            string label = string.IsNullOrWhiteSpace(StepTitle)
                ? StepId
                : StepTitle;

            if (string.IsNullOrWhiteSpace(label))
            {
                label = StepType;
            }

            if (StepIndex >= 0)
            {
                label = "Step " + (StepIndex + 1) + (string.IsNullOrWhiteSpace(label) ? "" : " (" + label + ")");
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                return Severity + ": " + Message;
            }

            return Severity + " — " + label + ": " + Message;
        }
    }
}
