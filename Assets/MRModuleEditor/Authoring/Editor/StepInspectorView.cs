using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class StepInspectorView
    {
        public static void Draw(ModuleDocument document, int selectedStepIndex)
        {
            EditorGUILayout.LabelField("Step Inspector", EditorStyles.boldLabel);

            if (document == null || document.steps == null || document.steps.Count == 0)
            {
                EditorGUILayout.HelpBox("No steps yet. Add a step from the left panel.", MessageType.Info);
                return;
            }

            if (selectedStepIndex < 0 || selectedStepIndex >= document.steps.Count)
            {
                EditorGUILayout.HelpBox("Select a step from the left panel.", MessageType.Info);
                return;
            }

            ModuleStep step = document.steps[selectedStepIndex];
            if (step == null)
            {
                EditorGUILayout.HelpBox("Selected step is null.", MessageType.Error);
                return;
            }

            if (step.parameters == null)
            {
                step.parameters = new Dictionary<string, JToken>();
            }

            StepCatalog catalog = StepCatalog.Global;
            List<StepTypeDefinition> definitions = catalog == null
                ? new List<StepTypeDefinition>()
                : catalog.GetDefinitions();

            step.id = EditorGUILayout.TextField("ID", step.id);
            DrawStepTypePopup(step, definitions);
            step.title = EditorGUILayout.TextField("Title", step.title);
            step.durationSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("Duration Seconds", step.durationSeconds));

            StepTypeDefinition definition = catalog == null ? null : catalog.Get(step.type);

            EditorGUILayout.Space(8);
            if (definition == null)
            {
                EditorGUILayout.HelpBox(
                    "Unknown step type: " + step.type + "\nRegister a StepTypeDefinition or choose a catalog type above.",
                    MessageType.Warning);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(definition.Description))
                {
                    EditorGUILayout.HelpBox(definition.Description, MessageType.Info);
                }

                GenericStepInspectorView.DrawParameters(document, step, definition);
            }

            EditorGUILayout.Space(8);
            DrawFlowFields(document, step);
        }

        public static void EnsureDefaultsForType(ModuleStep step)
        {
            if (step == null)
            {
                return;
            }

            if (step.parameters == null)
            {
                step.parameters = new Dictionary<string, JToken>();
            }

            StepCatalog catalog = StepCatalog.Global;
            if (catalog != null)
            {
                catalog.ApplyDefaults(step);
            }
        }

        private static void DrawStepTypePopup(ModuleStep step, List<StepTypeDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
            {
                EditorGUILayout.HelpBox("No step types are registered in the catalog.", MessageType.Error);
                return;
            }

            string[] labels = new string[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                StepTypeDefinition definition = definitions[i];
                labels[i] = definition.Category + "/" + definition.DisplayName;
            }

            int currentTypeIndex = FindTypeIndex(definitions, step.type);
            if (currentTypeIndex < 0)
            {
                EditorGUILayout.HelpBox(
                    "Unknown step type: " + step.type + "\nChoose a valid type from the popup to repair this step.",
                    MessageType.Warning);
            }

            int shownTypeIndex = currentTypeIndex < 0 ? 0 : currentTypeIndex;
            int nextTypeIndex = EditorGUILayout.Popup("Type", shownTypeIndex, labels);

            if (currentTypeIndex < 0 || nextTypeIndex != currentTypeIndex)
            {
                step.type = definitions[nextTypeIndex].Type;
                EnsureDefaultsForType(step);
            }
        }

        private static int FindTypeIndex(List<StepTypeDefinition> definitions, string stepType)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].Type == stepType)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void DrawFlowFields(ModuleDocument document, ModuleStep step)
        {
            EditorGUILayout.LabelField("Flow", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Leave fields empty to continue to the next step in the list. " +
                "Use flow overrides sparingly; this is branch-by-answer, not a full graph editor.",
                MessageType.Info);

            EditorIdDropdowns.DrawStepIdDropdown(
                document,
                step,
                "nextStepId",
                "Next Step Override",
                allowNone: true);
            RemoveParameterIfBlank(step, "nextStepId");

            if (step.type == "mcq")
            {
                EditorIdDropdowns.DrawStepIdDropdown(
                    document,
                    step,
                    "onCorrectStepId",
                    "On Correct",
                    allowNone: true);
                RemoveParameterIfBlank(step, "onCorrectStepId");

                EditorIdDropdowns.DrawStepIdDropdown(
                    document,
                    step,
                    "onWrongStepId",
                    "On Wrong",
                    allowNone: true);
                RemoveParameterIfBlank(step, "onWrongStepId");
            }
            else
            {
                bool hasMcqBranchField = !string.IsNullOrWhiteSpace(step.GetString("onCorrectStepId", ""))
                    || !string.IsNullOrWhiteSpace(step.GetString("onWrongStepId", ""));

                if (hasMcqBranchField)
                {
                    EditorGUILayout.HelpBox(
                        "This step has MCQ branch fields, but they are only used when Type is mcq.",
                        MessageType.Warning);
                }
            }
        }

        private static void RemoveParameterIfBlank(ModuleStep step, string key)
        {
            if (step == null || step.parameters == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(step.GetString(key, "")))
            {
                step.parameters.Remove(key);
            }
        }
    }
}
