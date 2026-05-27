using System.Collections.Generic;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class StepHandlerRegistry
    {
        private readonly Dictionary<string, IStepHandler> handlers = new Dictionary<string, IStepHandler>();

        public void Register(IStepHandler handler)
        {
            if (handler == null || string.IsNullOrWhiteSpace(handler.StepType))
            {
                return;
            }

            handlers[handler.StepType] = handler;
        }

        public bool TryGet(string stepType, out IStepHandler handler)
        {
            handler = null;

            if (string.IsNullOrWhiteSpace(stepType))
            {
                return false;
            }

            return handlers.TryGetValue(stepType, out handler);
        }

        public void RegisterDefaultHandlers()
        {
            BuiltInStepInstaller.RegisterHandlers(this);
        }
    }
}