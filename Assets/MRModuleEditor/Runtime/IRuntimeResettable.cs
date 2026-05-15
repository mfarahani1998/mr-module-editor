namespace MRModuleEditor.Runtime
{
    /// <summary>
    /// Optional runtime reset hook for components that hold step-owned state.
    ///
    /// ModuleRunner calls this during Stop/Restart/clean Play so domain-specific
    /// systems can return to their initial pose without being hard-coded into the
    /// core runner.
    /// </summary>
    public interface IRuntimeResettable
    {
        void ResetRuntimeState();
    }
}
