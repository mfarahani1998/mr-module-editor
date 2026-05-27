using System;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Validation;

namespace MRModuleEditor.Core.StepTypes
{
    public delegate void StepValidationDelegate(
        ModuleStep step,
        StepValidationContext context,
        string location,
        List<ValidationIssue> issues);

    public sealed class StepTypeDefinition
    {
        public StepTypeDefinition(
            string type,
            string displayName,
            string category,
            string description,
            bool supportsLayout,
            float defaultDurationSeconds,
            StepParameterDefinition[] parameters,
            StepValidationDelegate customValidator = null)
        {
            Type = type ?? "";
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Type : displayName;
            Category = string.IsNullOrWhiteSpace(category) ? "Other" : category;
            Description = description ?? "";
            SupportsLayout = supportsLayout;
            DefaultDurationSeconds = defaultDurationSeconds;
            Parameters = parameters ?? new StepParameterDefinition[0];
            CustomValidator = customValidator;
        }

        public string Type { get; private set; }
        public string DisplayName { get; private set; }
        public string Category { get; private set; }
        public string Description { get; private set; }
        public bool SupportsLayout { get; private set; }
        public float DefaultDurationSeconds { get; private set; }
        public StepParameterDefinition[] Parameters { get; private set; }
        public StepValidationDelegate CustomValidator { get; private set; }

        public void ApplyDefaults(ModuleStep step)
        {
            if (step == null)
            {
                return;
            }

            if (step.parameters == null)
            {
                step.parameters = new Dictionary<string, Newtonsoft.Json.Linq.JToken>();
            }

            if (string.IsNullOrWhiteSpace(step.title))
            {
                step.title = DisplayName;
            }

            if (step.durationSeconds <= 0f && DefaultDurationSeconds > 0f)
            {
                step.durationSeconds = DefaultDurationSeconds;
            }

            for (int i = 0; i < Parameters.Length; i++)
            {
                StepParameterDefinition parameter = Parameters[i];
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.Key))
                {
                    continue;
                }

                if (step.parameters.ContainsKey(parameter.Key))
                {
                    continue;
                }

                Newtonsoft.Json.Linq.JToken defaultValue = parameter.CloneDefaultValue();
                if (defaultValue != null)
                {
                    step.parameters[parameter.Key] = defaultValue;
                }
            }
        }
    }
}
