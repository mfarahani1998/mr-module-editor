using System;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class StepListView
    {
        public static int Draw(
            ModuleDocument document,
            int selectedStepIndex,
            Action<string> addStep,
            Action removeSelectedStep,
            Action moveSelectedStepUp,
            Action moveSelectedStepDown)
        {
            EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);

            if (document == null)
            {
                EditorGUILayout.HelpBox("No module loaded.", MessageType.Info);
                return -1;
            }

            if (document.steps == null)
            {
                document.steps = new System.Collections.Generic.List<ModuleStep>();
            }

            for (int i = 0; i < document.steps.Count; i++)
            {
                ModuleStep step = document.steps[i];
                string label = step == null
                    ? i + ": <null>"
                    : i + ": " + step.type + " — " + step.title;

                GUIStyle style = i == selectedStepIndex ? EditorStyles.toolbarButton : GUI.skin.button;
                if (GUILayout.Button(label, style))
                {
                    selectedStepIndex = i;
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Add Step", EditorStyles.boldLabel);
            DrawCatalogButtons(document, addStep, ref selectedStepIndex);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Selected Step", EditorStyles.boldLabel);

            GUI.enabled = selectedStepIndex >= 0 && selectedStepIndex < document.steps.Count;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Up"))
            {
                moveSelectedStepUp();
                selectedStepIndex = Mathf.Max(0, selectedStepIndex - 1);
            }

            if (GUILayout.Button("Down"))
            {
                moveSelectedStepDown();
                selectedStepIndex = Mathf.Min(document.steps.Count - 1, selectedStepIndex + 1);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Remove Selected"))
            {
                removeSelectedStep();
                selectedStepIndex = Mathf.Clamp(selectedStepIndex, 0, document.steps.Count - 1);
                if (document.steps.Count == 0) selectedStepIndex = -1;
            }

            GUI.enabled = true;

            return selectedStepIndex;
        }

        private static void DrawCatalogButtons(ModuleDocument document, Action<string> addStep, ref int selectedStepIndex)
        {
            StepCatalog catalog = StepCatalog.Global;
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("Step catalog is missing.", MessageType.Error);
                return;
            }

            List<StepTypeDefinition> definitions = catalog.GetDefinitions();
            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox("No step types are registered in the catalog.", MessageType.Warning);
                return;
            }

            string currentCategory = null;
            for (int i = 0; i < definitions.Count; i++)
            {
                StepTypeDefinition definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (currentCategory != definition.Category)
                {
                    currentCategory = definition.Category;
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(currentCategory, EditorStyles.miniBoldLabel);
                }

                if (GUILayout.Button(definition.DisplayName))
                {
                    selectedStepIndex = Add(addStep, document, definition.Type);
                }
            }
        }

        private static int Add(Action<string> addStep, ModuleDocument document, string stepType)
        {
            addStep(stepType);
            return document.steps.Count - 1;
        }
    }
}
