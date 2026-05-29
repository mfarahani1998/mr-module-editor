using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.Preview
{
    /// <summary>
    /// Optional companion interface for step handlers whose end-state side effects can be applied instantly
    /// before a Preview Selected run. Interactive, media, and time-based steps can safely omit this interface;
    /// the runner will skip them and report a warning instead of pretending their learner-dependent outcome is known.
    /// </summary>
    public interface IPreviewPreparationStepHandler
    {
        bool PrepareForPreview(ModuleStep step, RuntimeContext context, PreviewPreparationContext preparationContext, int stepIndex);
    }
}
