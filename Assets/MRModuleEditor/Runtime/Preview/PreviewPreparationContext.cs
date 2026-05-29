using System.Collections.Generic;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.Preview
{
    public sealed class PreviewPreparationContext
    {
        private readonly List<PreviewPreparationIssue> issues = new List<PreviewPreparationIssue>();

        public PreviewPreparationContext(RuntimeContext runtimeContext)
        {
            Runtime = runtimeContext;
        }

        public RuntimeContext Runtime { get; private set; }
        public int AppliedStepCount { get; private set; }
        public int SkippedStepCount { get; private set; }
        public int WarningCount { get; private set; }
        public int ErrorCount { get; private set; }

        public List<PreviewPreparationIssue> Issues
        {
            get { return issues; }
        }

        public bool HasErrors
        {
            get { return ErrorCount > 0; }
        }

        public void MarkApplied(ModuleStep step, int stepIndex, string message)
        {
            AppliedStepCount++;
            AddIssue(PreviewPreparationSeverity.Info, step, stepIndex, message);
        }

        public void MarkSkipped(ModuleStep step, int stepIndex, string reason)
        {
            SkippedStepCount++;
            AddIssue(PreviewPreparationSeverity.Warning, step, stepIndex, reason);
        }

        public void AddWarning(ModuleStep step, int stepIndex, string message)
        {
            AddIssue(PreviewPreparationSeverity.Warning, step, stepIndex, message);
        }

        public void AddError(ModuleStep step, int stepIndex, string message)
        {
            AddIssue(PreviewPreparationSeverity.Error, step, stepIndex, message);
        }

        private void AddIssue(PreviewPreparationSeverity severity, ModuleStep step, int stepIndex, string message)
        {
            if (severity == PreviewPreparationSeverity.Warning)
            {
                WarningCount++;
            }
            else if (severity == PreviewPreparationSeverity.Error)
            {
                ErrorCount++;
            }

            issues.Add(new PreviewPreparationIssue(severity, step, stepIndex, message));
        }
    }
}
