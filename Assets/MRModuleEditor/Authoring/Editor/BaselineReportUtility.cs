using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using MRModuleEditor.Core.Validation;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class BaselineReportUtility
    {
        private const string SampleModuleAssetPath =
            "Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json";

        private const string RuntimePreviewSceneAssetPath =
            "Assets/MRModuleEditor/Samples/Scenes/RuntimePreview.unity";

        private const string HeadsetPreviewSceneAssetPath =
            "Assets/MRModuleEditor/Samples/Scenes/RuntimePreview_Headset.unity";

        [MenuItem("MR Module Editor/Reports/Baseline")]
        public static void WriteBaselineReport()
        {
            const string outputDirectory = "Assets/MRModuleEditor/Docs/baseline";
            Directory.CreateDirectory(outputDirectory);

            string outputPath = Path.Combine(outputDirectory, "baseline-report.md");
            StringBuilder report = new StringBuilder();

            report.AppendLine("# MVP Baseline Report");
            report.AppendLine();
            report.AppendLine("Generated UTC: " + DateTime.UtcNow.ToString("u"));
            report.AppendLine("Unity version: " + Application.unityVersion);
            report.AppendLine();

            report.AppendLine("## Asset checks");
            AppendAssetCheck(report, "Sample module", SampleModuleAssetPath);
            AppendAssetCheck(report, "Runtime preview scene", RuntimePreviewSceneAssetPath);
            AppendAssetCheck(report, "Headset preview scene", HeadsetPreviewSceneAssetPath);
            report.AppendLine();

            AppendSampleModuleSummary(report);

            File.WriteAllText(outputPath, report.ToString());
            AssetDatabase.Refresh();

            Debug.Log("Wrote baseline report to " + outputPath);
            EditorUtility.DisplayDialog(
                "Baseline Report",
                "Wrote baseline report to:\n" + outputPath,
                "OK");
        }

        private static void AppendAssetCheck(StringBuilder report, string label, string assetPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            string status = string.IsNullOrEmpty(guid) ? "MISSING" : "found";
            report.AppendLine("- " + label + ": `" + assetPath + "` — " + status);
        }

        private static void AppendSampleModuleSummary(StringBuilder report)
        {
            report.AppendLine("## Sample module summary");

            string absolutePath = Path.GetFullPath(SampleModuleAssetPath);
            if (!File.Exists(absolutePath))
            {
                report.AppendLine("Sample module file is missing: `" + SampleModuleAssetPath + "`");
                return;
            }

            try
            {
                ModuleDocument document = ModuleJsonSerializer.LoadFromFile(absolutePath);
                List<ValidationIssue> issues = ModuleValidator.Validate(document);

                int errorCount = issues.Count(issue => issue.severity == ValidationSeverity.Error);
                int warningCount = issues.Count(issue => issue.severity == ValidationSeverity.Warning);

                report.AppendLine("- Module ID: `" + Safe(document.moduleId) + "`");
                report.AppendLine("- Title: " + Safe(document.title));
                report.AppendLine("- Schema version: `" + Safe(document.schemaVersion) + "`");
                report.AppendLine("- Assets: " + Count(document.assets));
                report.AppendLine("- Objects: " + Count(document.objects));
                report.AppendLine("- Anchors: " + Count(document.anchors));
                report.AppendLine("- Layouts: " + Count(document.layouts));
                report.AppendLine("- Steps: " + Count(document.steps));
                report.AppendLine("- Validation: " + errorCount + " error(s), " + warningCount + " warning(s)");
                report.AppendLine();

                AppendStepInventory(report, document);

                if (issues.Count > 0)
                {
                    report.AppendLine("## Validation issues");
                    foreach (ValidationIssue issue in issues)
                    {
                        report.AppendLine("- " + issue);
                    }
                    report.AppendLine();
                }
            }
            catch (Exception exception)
            {
                report.AppendLine("Failed to read sample module:");
                report.AppendLine();
                report.AppendLine("```text");
                report.AppendLine(exception.ToString());
                report.AppendLine("```");
            }
        }

        private static void AppendStepInventory(StringBuilder report, ModuleDocument document)
        {
            report.AppendLine("## Step inventory");
            report.AppendLine();
            report.AppendLine("| # | ID | Type | Title |");
            report.AppendLine("|---:|---|---|---|");

            if (document.steps == null)
            {
                return;
            }

            for (int i = 0; i < document.steps.Count; i++)
            {
                ModuleStep step = document.steps[i];
                if (step == null)
                {
                    report.AppendLine("| " + i + " | `<null>` |  |  |");
                    continue;
                }

                report.AppendLine(
                    "| " + i +
                    " | `" + EscapePipes(step.id) + "`" +
                    " | `" + EscapePipes(step.type) + "`" +
                    " | " + EscapePipes(step.title) + " |");
            }

            report.AppendLine();
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(empty)" : value;
        }

        private static int Count<T>(List<T> list)
        {
            return list == null ? 0 : list.Count;
        }

        private static string EscapePipes(string value)
        {
            return Safe(value).Replace("|", "\\|");
        }
    }
}

