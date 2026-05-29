using System.Collections.Generic;
using System.Text;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Preview;
using UnityEditor;

namespace MRModuleEditor.Authoring.Editor
{
    public static class EditorPreviewPreparationUtility
    {
        public static bool ConfirmPreviewSelected(ModuleDocument document, int selectedStepIndex, out bool previewFromStart)
        {
            previewFromStart = false;

            if (document == null || document.steps == null || selectedStepIndex <= 0)
            {
                return true;
            }

            string message = BuildPreviewSelectedMessage(document, selectedStepIndex);
            int choice = EditorUtility.DisplayDialogComplex(
                "Preview Selected Preparation",
                message,
                "Preview Selected",
                "Cancel",
                "Preview From Start");

            if (choice == 1)
            {
                return false;
            }

            if (choice == 2)
            {
                previewFromStart = true;
            }

            return true;
        }

        public static string BuildPreviewSelectedMessage(ModuleDocument document, int selectedStepIndex)
        {
            if (document == null || document.steps == null || selectedStepIndex <= 0)
            {
                return "Preview will start at the selected step.";
            }

            int deterministicCount = 0;
            int dynamicCount = 0;
            int flowCaveatCount = 0;
            List<string> caveats = new List<string>();

            for (int i = 0; i < selectedStepIndex && i < document.steps.Count; i++)
            {
                ModuleStep step = document.steps[i];
                if (IsDeterministicallyPreparedType(step == null ? "" : step.type))
                {
                    deterministicCount++;
                }
                else
                {
                    dynamicCount++;
                    if (caveats.Count < 4)
                    {
                        caveats.Add("- " + DescribeStep(step, i) + ": " + PreviewPreparationUtility.DescribeSkippedStep(step));
                    }
                }

                if (PreviewPreparationUtility.HasFlowCaveat(step))
                {
                    flowCaveatCount++;
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Preview Selected will no longer jump blindly to the selected step.");
            builder.AppendLine();
            builder.AppendLine("Before the selected step begins, the runtime will instantly prepare supported deterministic effects from earlier steps, including variables, object visibility, object transforms, highlights, callouts, and robotics pose/frame state.");
            builder.AppendLine();
            builder.AppendLine("Preparation summary for earlier editor-order steps:");
            builder.AppendLine("- Supported deterministic steps: " + deterministicCount);
            builder.AppendLine("- Dynamic/unsupported steps skipped with warnings: " + dynamicCount);
            builder.AppendLine("- Previous flow/branch caveats: " + flowCaveatCount);

            if (caveats.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Examples of caveats:");
                for (int i = 0; i < caveats.Count; i++)
                {
                    builder.AppendLine(caveats[i]);
                }
            }

            if (flowCaveatCount > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Branch guardrail: preparation uses the step list order up to the selected step. It does not choose MCQ answers, signal results, or alternate flow paths for the learner.");
            }

            builder.AppendLine();
            builder.AppendLine("Use Preview From Start when learner choices or branch history must be exact.");
            return builder.ToString();
        }

        private static bool IsDeterministicallyPreparedType(string type)
        {
            return type == "setVariable"
                || type == "showObject"
                || type == "highlightObject"
                || type == "showCallout"
                || type == "moveObject"
                || type == "resetRobot"
                || type == "rotateJoint"
                || type == "showFrame";
        }

        private static string DescribeStep(ModuleStep step, int index)
        {
            if (step == null)
            {
                return "Step " + (index + 1);
            }

            string label = string.IsNullOrWhiteSpace(step.title) ? step.id : step.title;
            if (string.IsNullOrWhiteSpace(label))
            {
                label = step.type;
            }

            return "Step " + (index + 1) + (string.IsNullOrWhiteSpace(label) ? "" : " (" + label + ")");
        }
    }
}
