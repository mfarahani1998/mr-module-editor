using System.Collections.Generic;
using System.IO;
using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using MRModuleEditor.Core.Templates;
using MRModuleEditor.Core.Utilities;
using MRModuleEditor.Core.Validation;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public class ModuleEditorWindow : EditorWindow
    {
        private const string CurrentPathSessionKey = "MRModuleEditor.Authoring.CurrentPath";
        private const string ReloadAfterPreviewKey = "MRModuleEditor.Authoring.ReloadAfterPreview";
        private ModuleDocument document;
        private string currentPath = "";
        private int selectedStepIndex = -1;
        private Vector2 leftScroll;
        private Vector2 rightScroll;
        private string status = "";
        private bool isDirty;

        [MenuItem("MR Module Editor/Phase C/Module Editor")]
        public static void Open()
        {
            ModuleEditorWindow window = GetWindow<ModuleEditorWindow>("MR Module Editor");
            window.minSize = new Vector2(900, 560);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            string savedPath = SessionState.GetString(CurrentPathSessionKey, "");
            if (!string.IsNullOrEmpty(savedPath))
            {
                currentPath = savedPath;
            }

            if (document == null)
            {
                if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                {
                    ReloadCurrentFile("Loaded module.");
                }
                else
                {
                    NewTemplate(false);
                }
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            if (!SessionState.GetBool(ReloadAfterPreviewKey, false))
            {
                return;
            }

            SessionState.SetBool(ReloadAfterPreviewKey, false);

            string savedPath = SessionState.GetString(CurrentPathSessionKey, "");
            if (!string.IsNullOrEmpty(savedPath))
            {
                currentPath = savedPath;
            }

            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
            {
                ReloadCurrentFile("Reloaded saved module after preview.");
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawCurrentPath();

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(
                    "Preview is running. Editing is disabled during Play Mode. Return to Edit Mode to continue editing the saved module.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUILayout.Width(330));
            selectedStepIndex = StepListView.Draw(
                document,
                selectedStepIndex,
                AddStep,
                RemoveSelectedStep,
                MoveSelectedStepUp,
                MoveSelectedStepDown);
            EditorGUILayout.EndScrollView();

            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
            DrawModuleMetadata();
            EditorGUILayout.Space(8);
            StepInspectorView.Draw(document, selectedStepIndex);
            EditorGUILayout.Space(8);
            DrawValidationSummary();
            DrawDataSummary();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.HelpBox(status, MessageType.Info);
            }

            if (GUI.changed)
            {
                isDirty = true;
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New Template", EditorStyles.toolbarButton))
            {
                if (ConfirmLoseUnsavedChanges()) NewTemplate(true);
            }

            if (GUILayout.Button("New Empty", EditorStyles.toolbarButton))
            {
                if (ConfirmLoseUnsavedChanges()) NewEmpty();
            }

            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                if (ConfirmLoseUnsavedChanges()) Load();
            }

            if (GUILayout.Button("Reload From Disk", EditorStyles.toolbarButton))
            {
                if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                {
                    ReloadCurrentFile("Reloaded module from disk.");
                }
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                Save();
            }

            if (GUILayout.Button("Save As", EditorStyles.toolbarButton))
            {
                SaveAs();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Preview", EditorStyles.toolbarButton))
            {
                Preview();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCurrentPath()
        {
            string dirtyMark = isDirty ? " *" : "";
            EditorGUILayout.LabelField("Current File" + dirtyMark, string.IsNullOrEmpty(currentPath) ? "Unsaved" : currentPath);
        }

        private void DrawModuleMetadata()
        {
            if (document == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Module Metadata", EditorStyles.boldLabel);
            document.schemaVersion = EditorGUILayout.TextField("Schema Version", document.schemaVersion);
            document.moduleId = EditorGUILayout.TextField("Module ID", document.moduleId);
            document.title = EditorGUILayout.TextField("Title", document.title);
            document.author = EditorGUILayout.TextField("Author", document.author);
            document.estimatedDurationSeconds = EditorGUILayout.IntField("Estimated Duration", document.estimatedDurationSeconds);

            EditorGUILayout.LabelField("Description");
            document.description = EditorGUILayout.TextArea(document.description, GUILayout.MinHeight(50));
        }

        private void DrawValidationSummary()
        {
            if (document == null)
            {
                return;
            }

            List<ValidationIssue> issues = ModuleValidator.Validate(document);
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Validation passed.", MessageType.Info);
                return;
            }

            ValidationSeverity worst = issues.Any(issue => issue.severity == ValidationSeverity.Error)
                ? ValidationSeverity.Error
                : ValidationSeverity.Warning;

            MessageType messageType = worst == ValidationSeverity.Error ? MessageType.Error : MessageType.Warning;
            string text = string.Join("\n", issues.Select(issue => issue.ToString()).ToArray());
            EditorGUILayout.HelpBox(text, messageType);
        }

        private void DrawDataSummary()
        {
            if (document == null)
            {
                return;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Template Data Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Assets", document.assets == null ? "0" : document.assets.Count.ToString());
            EditorGUILayout.LabelField("Objects", document.objects == null ? "0" : document.objects.Count.ToString());
            EditorGUILayout.LabelField("Anchors", document.anchors == null ? "0" : document.anchors.Count.ToString());
            EditorGUILayout.LabelField("Layouts", document.layouts == null ? "0" : document.layouts.Count.ToString());

            EditorGUILayout.HelpBox(
                "Phase C intentionally edits assets, objects, and anchors only through the template and ID text fields. A full asset browser comes later.",
                MessageType.Info);
        }

        private void NewTemplate(bool setDefaultPath)
        {
            document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            selectedStepIndex = document.steps.Count > 0 ? 0 : -1;
            isDirty = true;
            status = "Created Forward Kinematics Mini template.";

            if (setDefaultPath || string.IsNullOrEmpty(currentPath))
            {
                currentPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json");
                RememberCurrentPath();
            }
        }

        private void NewEmpty()
        {
            document = ModuleTemplateFactory.CreateEmptyModule();
            currentPath = "";
            selectedStepIndex = -1;
            isDirty = true;
            status = "Created empty module.";
        }

        private void Load()
        {
            string startDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            string path = EditorUtility.OpenFilePanel("Load module.json", startDirectory, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            currentPath = path;
            RememberCurrentPath();
            ReloadCurrentFile("Loaded module.");
        }

        private bool Save()
        {
            if (document == null)
            {
                status = "No module to save.";
                return false;
            }

            if (string.IsNullOrEmpty(currentPath))
            {
                return SaveAs();
            }

            ModuleJsonSerializer.SaveToFile(document, currentPath);
            AssetDatabase.Refresh();
            isDirty = false;
            status = "Saved module.";
            return true;
        }

        private bool SaveAs()
        {
            string startDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini");

            string path = EditorUtility.SaveFilePanel("Save module.json", startDirectory, "module.json", "json");
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            currentPath = path;
            RememberCurrentPath();
            return Save();
        }

        private void Preview()
        {
            if (!Save())
            {
                return;
            }

            List<ValidationIssue> issues = ModuleValidator.Validate(document);
            if (ModuleValidator.HasError(issues))
            {
                string text = string.Join("\n", issues.Select(issue => issue.ToString()).ToArray());
                EditorUtility.DisplayDialog("Cannot Preview", "Fix validation errors first:\n\n" + text, "OK");
                return;
            }

            RememberCurrentPath();
            SessionState.SetBool(ReloadAfterPreviewKey, true);
            ModulePreviewLauncher.LaunchPreview(currentPath);
        }

        private void AddStep(string type)
        {
            if (document == null)
            {
                NewEmpty();
            }

            ModuleStep step = new ModuleStep();
            step.id = IdGenerator.NewId("step");
            step.type = type;
            step.title = type;
            step.parameters = new Dictionary<string, JToken>();
            StepInspectorView.EnsureDefaultsForType(step);

            document.steps.Add(step);
            selectedStepIndex = document.steps.Count - 1;
            isDirty = true;
        }

        private void RemoveSelectedStep()
        {
            if (document == null || selectedStepIndex < 0 || selectedStepIndex >= document.steps.Count)
            {
                return;
            }

            document.steps.RemoveAt(selectedStepIndex);
            selectedStepIndex = Mathf.Clamp(selectedStepIndex, 0, document.steps.Count - 1);
            if (document.steps.Count == 0) selectedStepIndex = -1;
            isDirty = true;
        }

        private void MoveSelectedStepUp()
        {
            if (document == null || selectedStepIndex <= 0 || selectedStepIndex >= document.steps.Count)
            {
                return;
            }

            ModuleStep temp = document.steps[selectedStepIndex - 1];
            document.steps[selectedStepIndex - 1] = document.steps[selectedStepIndex];
            document.steps[selectedStepIndex] = temp;
            selectedStepIndex--;
            isDirty = true;
        }

        private void MoveSelectedStepDown()
        {
            if (document == null || selectedStepIndex < 0 || selectedStepIndex >= document.steps.Count - 1)
            {
                return;
            }

            ModuleStep temp = document.steps[selectedStepIndex + 1];
            document.steps[selectedStepIndex + 1] = document.steps[selectedStepIndex];
            document.steps[selectedStepIndex] = temp;
            selectedStepIndex++;
            isDirty = true;
        }

        private bool ConfirmLoseUnsavedChanges()
        {
            if (!isDirty)
            {
                return true;
            }

            return EditorUtility.DisplayDialog(
                "Unsaved changes",
                "Continue and lose unsaved changes?",
                "Continue",
                "Cancel");
        }

        private void RememberCurrentPath()
        {
            SessionState.SetString(CurrentPathSessionKey, currentPath ?? "");
        }

        private void ReloadCurrentFile(string message)
        {
            document = ModuleJsonSerializer.LoadFromFile(currentPath);
            selectedStepIndex = document.steps.Count > 0
                ? Mathf.Clamp(selectedStepIndex, 0, document.steps.Count - 1)
                : -1;
            isDirty = false;
            status = message;
        }
    }
}