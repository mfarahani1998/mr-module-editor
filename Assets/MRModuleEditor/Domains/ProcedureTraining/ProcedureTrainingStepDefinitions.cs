using System;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using MRModuleEditor.Core.Validation;
using UnityEngine;

namespace MRModuleEditor.Domains.ProcedureTraining
{
    public static class ProcedureTrainingStepDefinitions
    {
        private const string Category = "Procedure Training";

        private static readonly HashSet<string> KnownSafetyStatuses = new HashSet<string>(StringComparer.Ordinal)
        {
            "Checked",
            "NeedsAttention",
            "Blocked"
        };

        public static void Register()
        {
            Register(StepCatalog.Global);
        }

        public static void Register(StepCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            catalog.Register(new StepTypeDefinition(
                "showProcedureItem",
                "Show Procedure Item",
                Category,
                "Reveals a procedure item, records it as the current item, and optionally highlights it.",
                true,
                3f,
                new[]
                {
                    new StepParameterDefinition("objectId", "Procedure Object", StepParameterKind.ObjectId, true, "object.equipment_demo"),
                    new StepParameterDefinition("itemId", "Item ID", StepParameterKind.String, true, "guard-door"),
                    new StepParameterDefinition("instruction", "Instruction", StepParameterKind.MultilineString, true, "Inspect this procedure item."),
                    new StepParameterDefinition("highlight", "Highlight", StepParameterKind.Bool, false, true),
                    new StepParameterDefinition("colorHex", "Highlight Color", StepParameterKind.String, false, "#FFD54F")
                        .VisibleWhenBool("highlight", true),
                    new StepParameterDefinition("pulseAmplitude", "Pulse Amplitude", StepParameterKind.Float, false, 0.05f)
                        .VisibleWhenBool("highlight", true),
                    new StepParameterDefinition("pulseSeconds", "Pulse Seconds", StepParameterKind.Float, false, 0.8f)
                        .VisibleWhenBool("highlight", true),
                    new StepParameterDefinition("clearHighlightOnComplete", "Clear Highlight On Complete", StepParameterKind.Bool, false, false)
                        .VisibleWhenBool("highlight", true),
                    new StepParameterDefinition("resultVariableKey", "Result Variable Key", StepParameterKind.String, false, "procedure.currentItem")
                },
                ValidateShowProcedureItem));

            catalog.Register(new StepTypeDefinition(
                "checkSafetyPoint",
                "Check Safety Point",
                Category,
                "Shows a learner-paced safety checkpoint and records a simple safety status.",
                true,
                0f,
                new[]
                {
                    new StepParameterDefinition("objectId", "Procedure Object", StepParameterKind.ObjectId, true, "object.equipment_demo"),
                    new StepParameterDefinition("safetyPointId", "Safety Point ID", StepParameterKind.String, true, "guard-door-closed"),
                    new StepParameterDefinition("prompt", "Prompt", StepParameterKind.MultilineString, true, "Confirm the safety point before continuing."),
                    new StepParameterDefinition("buttonLabel", "Button Label", StepParameterKind.String, false, "Mark Checked"),
                    new StepParameterDefinition("status", "Status To Record", StepParameterKind.Choice, false, "Checked")
                        .WithChoices("Checked", "NeedsAttention", "Blocked"),
                    new StepParameterDefinition("autoCompleteAfterSeconds", "Auto Complete After Seconds", StepParameterKind.Float, false, 0f),
                    new StepParameterDefinition("resultVariableKey", "Result Variable Key", StepParameterKind.String, false, "procedure.safetyPoint.status"),
                    new StepParameterDefinition("highlightOnComplete", "Highlight On Complete", StepParameterKind.Bool, false, false),
                    new StepParameterDefinition("highlightColorHex", "Completion Highlight Color", StepParameterKind.String, false, "#66BB6A")
                        .VisibleWhenBool("highlightOnComplete", true)
                },
                ValidateCheckSafetyPoint));
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterForEditor()
        {
            Register();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterForRuntime()
        {
            Register();
        }

        private static void ValidateShowProcedureItem(ModuleStep step, StepValidationContext context, string location, List<ValidationIssue> issues)
        {
            if (step.GetBool("highlight", true))
            {
                ValidateColor(step.GetString("colorHex", ""), "procedureTraining.colorHex.invalid", "Highlight color must be a valid HTML color such as #FFD54F.", location, issues);
                ValidateNonNegative(step, "pulseAmplitude", "procedureTraining.pulseAmplitude.negative", location, issues);
                ValidateStrictlyPositive(step, "pulseSeconds", "procedureTraining.pulseSeconds.nonPositive", location, issues);
            }
        }

        private static void ValidateCheckSafetyPoint(ModuleStep step, StepValidationContext context, string location, List<ValidationIssue> issues)
        {
            string status = step.GetString("status", "Checked");
            if (!KnownSafetyStatuses.Contains(status))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "procedureTraining.status.unknown",
                    "Safety point status must be Checked, NeedsAttention, or Blocked.",
                    location));
            }

            ValidateNonNegative(step, "autoCompleteAfterSeconds", "procedureTraining.autoCompleteAfterSeconds.negative", location, issues);

            if (step.GetBool("highlightOnComplete", false))
            {
                ValidateColor(step.GetString("highlightColorHex", ""), "procedureTraining.highlightColorHex.invalid", "Completion highlight color must be a valid HTML color such as #66BB6A.", location, issues);
            }
        }

        private static void ValidateColor(string colorHex, string code, string message, string location, List<ValidationIssue> issues)
        {
            Color parsed;
            if (string.IsNullOrWhiteSpace(colorHex) || !ColorUtility.TryParseHtmlString(colorHex, out parsed))
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, code, message, location));
            }
        }

        private static void ValidateNonNegative(ModuleStep step, string key, string code, string location, List<ValidationIssue> issues)
        {
            if (step.GetFloat(key, 0f) < 0f)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    code,
                    key + " cannot be negative.",
                    location));
            }
        }

        private static void ValidateStrictlyPositive(ModuleStep step, string key, string code, string location, List<ValidationIssue> issues)
        {
            if (step.GetFloat(key, 1f) <= 0f)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    code,
                    key + " must be greater than zero.",
                    location));
            }
        }
    }
}
