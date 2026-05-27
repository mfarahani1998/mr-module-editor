namespace MRModuleEditor.Runtime.StepHandlers
{
    public static class BuiltInStepInstaller
    {
        public static void RegisterHandlers(StepHandlerRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new TextStepHandler());
            registry.Register(new WaitStepHandler());
            registry.Register(new ConfirmStepHandler());
            registry.Register(new ShowObjectStepHandler());
            registry.Register(new MoveObjectStepHandler());
            registry.Register(new ImageStepHandler());
            registry.Register(new MCQStepHandler());
            registry.Register(new AudioStepHandler());
        }
    }
}
