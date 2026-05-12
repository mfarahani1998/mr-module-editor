using System;

namespace MRModuleEditor.Core.Validation
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    [Serializable]
    public class ValidationIssue
    {
        public ValidationSeverity severity;
        public string code;
        public string message;
        public string location;

        public ValidationIssue(ValidationSeverity severity, string code, string message, string location = "")
        {
            this.severity = severity;
            this.code = code;
            this.message = message;
            this.location = location;
        }

        public override string ToString()
        {
            string suffix = string.IsNullOrEmpty(location) ? "" : " (" + location + ")";
            return "[" + severity + "] " + code + ": " + message + suffix;
        }
    }
}