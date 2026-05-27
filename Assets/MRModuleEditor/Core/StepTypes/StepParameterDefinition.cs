using Newtonsoft.Json.Linq;

namespace MRModuleEditor.Core.StepTypes
{
    public sealed class StepParameterDefinition
    {
        public StepParameterDefinition(
            string key,
            string displayName,
            StepParameterKind kind,
            bool required,
            object defaultValue = null,
            string description = "")
        {
            Key = key ?? "";
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Key : displayName;
            Kind = kind;
            Required = required;
            Description = description ?? "";
            Choices = new string[0];
            ExpectedAssetType = "";

            if (defaultValue != null)
            {
                DefaultValue = defaultValue as JToken;
                if (DefaultValue == null)
                {
                    DefaultValue = JToken.FromObject(defaultValue);
                }
            }
        }

        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public StepParameterKind Kind { get; private set; }
        public bool Required { get; private set; }
        public string Description { get; private set; }
        public string ExpectedAssetType { get; private set; }
        public string[] Choices { get; private set; }
        public JToken DefaultValue { get; private set; }
        public bool HasVisibilityCondition { get; private set; }
        public string VisibleWhenParameterKey { get; private set; }
        public bool VisibleWhenBoolValue { get; private set; }

        public StepParameterDefinition WithAssetType(string expectedAssetType)
        {
            ExpectedAssetType = expectedAssetType ?? "";
            return this;
        }

        public StepParameterDefinition WithChoices(params string[] choices)
        {
            Choices = choices ?? new string[0];
            return this;
        }

        public StepParameterDefinition VisibleWhenBool(string parameterKey, bool value)
        {
            HasVisibilityCondition = true;
            VisibleWhenParameterKey = parameterKey ?? "";
            VisibleWhenBoolValue = value;
            return this;
        }

        public JToken CloneDefaultValue()
        {
            return DefaultValue == null ? null : DefaultValue.DeepClone();
        }
    }
}
