using System;
using MRModuleEditor.Core.Models;
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

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Text")) selectedStepIndex = Add(addStep, document, "text");
            if (GUILayout.Button("Image")) selectedStepIndex = Add(addStep, document, "image");
            if (GUILayout.Button("Audio")) selectedStepIndex = Add(addStep, document, "audio");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Wait")) selectedStepIndex = Add(addStep, document, "wait");
            if (GUILayout.Button("Show Object")) selectedStepIndex = Add(addStep, document, "showObject");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Object")) selectedStepIndex = Add(addStep, document, "moveObject");
            if (GUILayout.Button("MCQ")) selectedStepIndex = Add(addStep, document, "mcq");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Frame")) selectedStepIndex = Add(addStep, document, "showFrame");
            if (GUILayout.Button("Rotate Joint")) selectedStepIndex = Add(addStep, document, "rotateJoint");
            if (GUILayout.Button("Reset Robot")) selectedStepIndex = Add(addStep, document, "resetRobot");
            EditorGUILayout.EndHorizontal();

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

        private static int Add(Action<string> addStep, ModuleDocument document, string stepType)
        {
            addStep(stepType);
            return document.steps.Count - 1;
        }
    }
}