using System;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Validation;

namespace MRModuleEditor.Core.StepTypes
{
    public sealed class StepCatalog
    {
        private static readonly StepCatalog global = StepCatalogBuilder.CreateDefaultCatalog();
        private readonly Dictionary<string, StepTypeDefinition> definitionsByType = new Dictionary<string, StepTypeDefinition>(StringComparer.Ordinal);

        public static StepCatalog Global
        {
            get { return global; }
        }

        public bool Register(StepTypeDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Type))
            {
                return false;
            }

            definitionsByType[definition.Type] = definition;
            return true;
        }

        public bool TryGet(string stepType, out StepTypeDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(stepType))
            {
                return false;
            }

            return definitionsByType.TryGetValue(stepType, out definition);
        }

        public StepTypeDefinition Get(string stepType)
        {
            StepTypeDefinition definition;
            return TryGet(stepType, out definition) ? definition : null;
        }

        public List<StepTypeDefinition> GetDefinitions()
        {
            List<StepTypeDefinition> definitions = new List<StepTypeDefinition>(definitionsByType.Values);
            definitions.Sort(CompareDefinitions);
            return definitions;
        }

        public string[] GetStepTypes()
        {
            List<StepTypeDefinition> definitions = GetDefinitions();
            string[] result = new string[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                result[i] = definitions[i].Type;
            }

            return result;
        }

        public void ApplyDefaults(ModuleStep step)
        {
            if (step == null)
            {
                return;
            }

            StepTypeDefinition definition;
            if (TryGet(step.type, out definition))
            {
                definition.ApplyDefaults(step);
            }
        }

        public void ValidateStepDefinitionHealth(List<ValidationIssue> issues)
        {
            if (issues == null)
            {
                return;
            }

            foreach (StepTypeDefinition definition in definitionsByType.Values)
            {
                if (string.IsNullOrWhiteSpace(definition.Type))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "stepCatalog.type.missing",
                        "A step definition is missing its type."));
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "stepCatalog.displayName.missing",
                        "Step definition '" + definition.Type + "' is missing a display name."));
                }
            }
        }

        private static int CompareDefinitions(StepTypeDefinition a, StepTypeDefinition b)
        {
            int category = string.Compare(a.Category, b.Category, StringComparison.Ordinal);
            if (category != 0)
            {
                return category;
            }

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        }
    }
}
