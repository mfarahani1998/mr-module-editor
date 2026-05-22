using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialMCQPanel : MonoBehaviour
    {
        private enum GazeDwellMode
        {
            Disabled,
            HeadsetOnly,
            Always
        }

        private class ChoiceVisual
        {
            public GameObject card;
            public TextMesh text;
            public Renderer renderer;
            public float height;
        }

        private const float LocalZBackground = 0f;
        private const float LocalZAccent = 0.005f;
        private const float LocalZChoiceCard = 0.01f;
        private const float LocalZTitleQuestion = 0.02f;
        private const float LocalZChoiceText = 0.03f;
        private const float LocalZFeedbackText = 0.03f;
        private const float ColliderDepth = 0.04f;

        private const int SortingBackground = 0;
        private const int SortingAccent = 1;
        private const int SortingChoiceCard = 1;
        private const int SortingText = 2;

        [SerializeField]
        private SpatialLayoutResolver spatialLayoutResolver;

        [Header("Panel")]
        [SerializeField]
        private bool autoSizePanel = true;

        // Used as the fixed size when autoSizePanel is disabled.
        [SerializeField]
        private Vector2 panelSize = new Vector2(1.75f, 1.45f);

        [FormerlySerializedAs("defaultLocalOffset")]
        [SerializeField]
        private Vector3 panelLocalOffset = new Vector3(0f, -0.15f, 0f);

        [SerializeField]
        private bool applyPanelLocalOffsetToAuthoredLayouts = false;

        [SerializeField]
        private Vector2 minimumPanelSize = new Vector2(1.0f, 0.65f);

        [SerializeField]
        private Vector2 maximumPanelSize = new Vector2(2.1f, 2.0f);

        [SerializeField]
        private Vector2 padding = new Vector2(0.08f, 0.08f);

        [SerializeField]
        private float textDepthOffset = 0.02f;

        [SerializeField]
        private Color panelColor = new Color(0.03f, 0.03f, 0.03f, 0.90f);

        [SerializeField]
        private bool showAccentBar = false;

        [SerializeField]
        private float accentWidth = 0.035f;

        [SerializeField]
        private float accentGap = 0.08f;

        [SerializeField]
        private Color accentColor = new Color(0.28f, 0.68f, 1.0f, 0.95f);

        [Header("Text")]
        [SerializeField]
        private int wrapCharacters = 50;

        [SerializeField]
        private int textFontSize = 32;

        [SerializeField]
        private float titleCharacterSize = 0.022f;

        [SerializeField]
        private float questionCharacterSize = 0.03f;

        [SerializeField]
        private float choiceCharacterSize = 0.022f;

        [SerializeField]
        private float feedbackCharacterSize = 0.022f;

        [SerializeField]
        private float titleLineHeight = 0.11f;

        [SerializeField]
        private float questionLineHeight = 0.14f;

        [SerializeField]
        private float choiceLineHeight = 0.065f;

        [SerializeField]
        private float feedbackLineHeight = 0.095f;

        [SerializeField]
        private float titleQuestionGap = 0.055f;

        [SerializeField]
        private float questionChoiceGap = 0.09f;

        [SerializeField]
        private float feedbackGapBelowChoices = 0.045f;

        // TextMesh is not layout-aware. This multiplier is an intentionally simple
        // approximation used to size primitive backgrounds from character counts.
        [SerializeField]
        private float estimatedCharacterWidth = 1.8f;

        [SerializeField]
        private Color titleColor = Color.white;

        [SerializeField]
        private Color questionColor = new Color(0.94f, 0.94f, 0.94f, 1f);

        [SerializeField]
        private Color choiceTextColor = Color.white;

        [SerializeField]
        private Color feedbackColor = new Color(0.82f, 0.90f, 1f, 1f);

        [Header("Choices")]
        [SerializeField]
        private bool autoSizeChoiceCards = true;

        // Used as the fixed choice height when autoSizeChoiceCards is disabled.
        [SerializeField]
        private float choiceHeight = 0.18f;

        [SerializeField]
        private float minimumChoiceHeight = 0.18f;

        [SerializeField]
        private float choiceVerticalPadding = 0.075f;

        [SerializeField]
        private float choiceGap = 0.04f;

        [SerializeField]
        private float choiceTextInsetX = 0.055f;

        [SerializeField]
        private float choiceTextTopOffset = 0.045f;

        [SerializeField]
        private Color choiceColor = new Color(0.16f, 0.20f, 0.26f, 0.96f);

        [SerializeField]
        private Color gazeColor = new Color(0.25f, 0.42f, 0.72f, 0.96f);

        [SerializeField]
        private Color selectedColor = new Color(0.20f, 0.55f, 0.35f, 0.96f);

        [SerializeField]
        private Color wrongColor = new Color(0.62f, 0.20f, 0.20f, 0.96f);

        [Header("Input")]
        [SerializeField]
        private bool enableGazeDwell = true;

        [SerializeField]
        private GazeDwellMode gazeDwellMode = GazeDwellMode.HeadsetOnly;

        [SerializeField]
        private float gazeDwellSeconds = 1.0f;

        [SerializeField]
        private float gazeInputArmDelaySeconds = 0.35f;

        [SerializeField]
        private bool requireFreshGazeTarget = true;

        [SerializeField]
        private bool lockHeadAnchoredPanelForGaze = true;

        [SerializeField]
        private float gazeRayDistance = 10f;

        [SerializeField]
        private bool enableKeyboardNumbers = true;

        [Header("Head Follow")]
        [SerializeField]
        private bool smoothFollow = true;

        [SerializeField]
        private float followSharpness = 16f;

        [SerializeField]
        private float snapDistance = 2.5f;

        private GameObject background;
        private GameObject accentBar;
        private TextMesh titleText;
        private TextMesh questionText;
        private TextMesh feedbackText;
        private readonly List<ChoiceVisual> choiceVisuals = new List<ChoiceVisual>();

        private ModuleDocument currentModule;
        private ModuleStep currentStep;
        private string showingStepId = "";
        private string[] choices = new string[0];
        private int correctIndex = -1;
        private int selectedIndex = -1;
        private int gazedIndex = -1;
        private float gazeTimer;
        private float shownTime;
        private bool gazeEnabledForCurrentQuestion;
        private bool gazeNeedsFreshTarget;
        private bool poseLockedForCurrentQuestion;
        private bool hasAppliedPose;

        private SpatialLayoutResolver LayoutResolver
        {
            get
            {
                if (spatialLayoutResolver == null)
                {
                    spatialLayoutResolver = FindFirstObjectByType<SpatialLayoutResolver>();
                }

                return spatialLayoutResolver;
            }
        }

        public bool HasAnswer
        {
            get { return selectedIndex >= 0; }
        }

        public int SelectedAnswer
        {
            get { return selectedIndex; }
        }

        private void Awake()
        {
            EnsureBaseVisuals();
            Clear();
        }

        private void OnValidate()
        {
            panelSize = SpatialRenderUtility.ClampVector(panelSize, new Vector2(0.1f, 0.1f));
            minimumPanelSize = SpatialRenderUtility.ClampVector(minimumPanelSize, new Vector2(0.1f, 0.1f));
            maximumPanelSize = new Vector2(
                Mathf.Max(maximumPanelSize.x, minimumPanelSize.x),
                Mathf.Max(maximumPanelSize.y, minimumPanelSize.y));
            padding = SpatialRenderUtility.ClampVector(padding, Vector2.zero);
            textDepthOffset = Mathf.Max(0.001f, textDepthOffset);
            wrapCharacters = Mathf.Max(1, wrapCharacters);
            textFontSize = Mathf.Max(1, textFontSize);
            titleCharacterSize = Mathf.Max(0.001f, titleCharacterSize);
            questionCharacterSize = Mathf.Max(0.001f, questionCharacterSize);
            choiceCharacterSize = Mathf.Max(0.001f, choiceCharacterSize);
            feedbackCharacterSize = Mathf.Max(0.001f, feedbackCharacterSize);
            titleLineHeight = Mathf.Max(0.001f, titleLineHeight);
            questionLineHeight = Mathf.Max(0.001f, questionLineHeight);
            choiceLineHeight = Mathf.Max(0.001f, choiceLineHeight);
            feedbackLineHeight = Mathf.Max(0.001f, feedbackLineHeight);
            titleQuestionGap = Mathf.Max(0f, titleQuestionGap);
            questionChoiceGap = Mathf.Max(0f, questionChoiceGap);
            feedbackGapBelowChoices = Mathf.Max(0f, feedbackGapBelowChoices);
            estimatedCharacterWidth = Mathf.Max(0.001f, estimatedCharacterWidth);
            accentWidth = Mathf.Max(0f, accentWidth);
            accentGap = Mathf.Max(0f, accentGap);
            choiceHeight = Mathf.Max(0.02f, choiceHeight);
            minimumChoiceHeight = Mathf.Max(0.02f, minimumChoiceHeight);
            choiceVerticalPadding = Mathf.Max(0f, choiceVerticalPadding);
            choiceGap = Mathf.Max(0f, choiceGap);
            choiceTextInsetX = Mathf.Max(0f, choiceTextInsetX);
            choiceTextTopOffset = Mathf.Max(0f, choiceTextTopOffset);
            gazeDwellSeconds = Mathf.Max(0.05f, gazeDwellSeconds);
            gazeInputArmDelaySeconds = Mathf.Max(0f, gazeInputArmDelaySeconds);
            gazeRayDistance = Mathf.Max(0.1f, gazeRayDistance);
            followSharpness = Mathf.Max(0.01f, followSharpness);
            snapDistance = Mathf.Max(0.01f, snapDistance);

            if (background != null && titleText != null && questionText != null && feedbackText != null)
            {
                ApplyTextSettings();
                UpdateMaterialColors();
                UpdateVisualLayout();
            }
        }

        public void ShowMCQ(
            ModuleDocument module,
            ModuleStep step,
            string question,
            string[] newChoices,
            int newCorrectIndex)
        {
            EnsureBaseVisuals();

            currentModule = module;
            currentStep = step;
            showingStepId = step == null ? "" : step.id;
            choices = newChoices ?? new string[0];
            correctIndex = newCorrectIndex;
            selectedIndex = -1;
            gazedIndex = -1;
            gazeTimer = 0f;
            shownTime = Time.time;
            gazeEnabledForCurrentQuestion = IsGazeDwellAvailable();
            gazeNeedsFreshTarget = requireFreshGazeTarget && gazeEnabledForCurrentQuestion;
            poseLockedForCurrentQuestion = false;
            hasAppliedPose = false;

            titleText.text = SpatialRenderUtility.Wrap(step == null ? "Quick Check" : step.title ?? "Quick Check", GetEffectiveWrapCharacters(titleCharacterSize, 0f));
            questionText.text = SpatialRenderUtility.Wrap(question ?? "", GetEffectiveWrapCharacters(questionCharacterSize, 0f));
            feedbackText.text = SpatialRenderUtility.Wrap(BuildInputInstruction(), GetEffectiveWrapCharacters(feedbackCharacterSize, 0f));

            ApplyTextSettings();
            UpdateMaterialColors();
            RebuildChoiceVisuals();
            UpdateVisualLayout();
            gameObject.SetActive(true);

            bool poseApplied = ApplyAnchoredPose();
            poseLockedForCurrentQuestion = poseApplied
                && gazeEnabledForCurrentQuestion
                && lockHeadAnchoredPanelForGaze
                && IsCurrentStepHeadAnchored();
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = SpatialRenderUtility.Wrap(message ?? "", GetEffectiveWrapCharacters(feedbackCharacterSize, 0f));
                UpdateVisualLayout();
            }
        }

        public void SubmitAnswer(int answerIndex)
        {
            if (selectedIndex >= 0)
            {
                return;
            }

            if (answerIndex < 0 || answerIndex >= choices.Length)
            {
                return;
            }

            selectedIndex = answerIndex;
            UpdateChoiceColors();
        }

        public void ClearIfShowingStep(string stepId)
        {
            if (string.IsNullOrEmpty(stepId) || showingStepId != stepId)
            {
                return;
            }

            Clear();
        }

        public void Clear()
        {
            showingStepId = "";
            currentModule = null;
            currentStep = null;
            choices = new string[0];
            correctIndex = -1;
            selectedIndex = -1;
            gazedIndex = -1;
            gazeTimer = 0f;
            shownTime = 0f;
            gazeEnabledForCurrentQuestion = false;
            gazeNeedsFreshTarget = false;
            poseLockedForCurrentQuestion = false;
            hasAppliedPose = false;

            if (titleText != null) titleText.text = "";
            if (questionText != null) questionText.text = "";
            if (feedbackText != null) feedbackText.text = "";

            ClearChoiceVisuals();
            if (background != null) UpdateVisualLayout();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || selectedIndex >= 0)
            {
                return;
            }

            if (enableKeyboardNumbers)
            {
                UpdateKeyboardNumbers();
            }

            if (gazeEnabledForCurrentQuestion)
            {
                UpdateGazeDwell();
            }
        }

        private void LateUpdate()
        {
            if (poseLockedForCurrentQuestion)
            {
                return;
            }

            bool poseApplied = ApplyAnchoredPose();
            if (poseApplied
                && gazeEnabledForCurrentQuestion
                && lockHeadAnchoredPanelForGaze
                && IsCurrentStepHeadAnchored())
            {
                poseLockedForCurrentQuestion = true;
            }
        }

        private bool ApplyAnchoredPose()
        {
            if (currentModule == null || currentStep == null)
            {
                return false;
            }

            SpatialLayoutResolver resolver = LayoutResolver;
            if (resolver == null)
            {
                return false;
            }

            Pose targetPose;
            Vector3 targetScale;
            string error;

            string fallbackAnchorId = currentStep.GetString("anchorId", "anchor.head.default");
            bool ok = resolver.TryResolvePoseForStep(
                currentModule,
                currentStep,
                fallbackAnchorId,
                panelLocalOffset,
                Vector3.zero,
                Vector3.one,
                applyPanelLocalOffsetToAuthoredLayouts,
                out targetPose,
                out targetScale,
                out error);

            if (!ok)
            {
                return false;
            }

            ApplyResolvedPose(targetPose, targetScale);
            return true;
        }

        private void ApplyResolvedPose(Pose targetPose, Vector3 targetScale)
        {
            transform.localScale = targetScale;

            if (!smoothFollow || !Application.isPlaying || !hasAppliedPose)
            {
                transform.position = targetPose.position;
                transform.rotation = targetPose.rotation;
                hasAppliedPose = true;
                return;
            }

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPose.position) > snapDistance)
            {
                transform.position = targetPose.position;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPose.position, t);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetPose.rotation, t);
        }

        private void UpdateKeyboardNumbers()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SubmitAnswer(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SubmitAnswer(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SubmitAnswer(2);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SubmitAnswer(3);
        }

        private void UpdateGazeDwell()
        {
            if (Time.time - shownTime < Mathf.Max(0f, gazeInputArmDelaySeconds))
            {
                gazedIndex = -1;
                gazeTimer = 0f;
                UpdateChoiceColors();
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            int hitIndex = FindChoiceHitIndex(ray);

            if (hitIndex < 0)
            {
                gazedIndex = -1;
                gazeTimer = 0f;
                gazeNeedsFreshTarget = false;
                UpdateChoiceColors();
                return;
            }

            if (gazeNeedsFreshTarget)
            {
                gazedIndex = -1;
                gazeTimer = 0f;
                UpdateChoiceColors();
                return;
            }

            if (hitIndex != gazedIndex)
            {
                gazedIndex = hitIndex;
                gazeTimer = 0f;
            }

            gazeTimer += Time.deltaTime;
            UpdateChoiceColors();

            if (gazeTimer >= gazeDwellSeconds)
            {
                SubmitAnswer(gazedIndex);
            }
        }

        private int FindChoiceHitIndex(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, gazeRayDistance);
            int hitIndex = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                int index = IndexOfChoiceCard(collider == null ? null : collider.gameObject);
                if (index < 0 || hits[i].distance >= closestDistance)
                {
                    continue;
                }

                hitIndex = index;
                closestDistance = hits[i].distance;
            }

            return hitIndex;
        }

        private bool IsGazeDwellAvailable()
        {
            if (!enableGazeDwell || gazeDwellMode == GazeDwellMode.Disabled)
            {
                return false;
            }

            if (gazeDwellMode == GazeDwellMode.Always)
            {
                return true;
            }

#if UNITY_EDITOR
            return false;
#else
            return XRSettings.isDeviceActive;
#endif
        }

        private string BuildInputInstruction()
        {
            string keyboardInstruction = BuildKeyboardInstruction();
            bool hasKeyboardInstruction = !string.IsNullOrEmpty(keyboardInstruction);

            if (gazeEnabledForCurrentQuestion && hasKeyboardInstruction)
            {
                return "Look away once, then look at an answer for "
                    + gazeDwellSeconds.ToString("0.0")
                    + "s; or "
                    + keyboardInstruction;
            }

            if (gazeEnabledForCurrentQuestion)
            {
                return "Look away once, then look at an answer for "
                    + gazeDwellSeconds.ToString("0.0")
                    + "s.";
            }

            if (hasKeyboardInstruction)
            {
                return UppercaseFirst(keyboardInstruction);
            }

            return "Choose an answer.";
        }

        private string BuildKeyboardInstruction()
        {
            if (!enableKeyboardNumbers || choices == null || choices.Length <= 0)
            {
                return "";
            }

            int keyboardChoiceCount = Mathf.Min(choices.Length, 4);
            if (keyboardChoiceCount == 1)
            {
                return "press 1 to answer.";
            }

            return "press 1-" + keyboardChoiceCount + " to answer.";
        }

        private static string UppercaseFirst(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? "";
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private bool IsCurrentStepHeadAnchored()
        {
            SpatialLayoutResolver resolver = LayoutResolver;
            if (resolver == null || currentStep == null)
            {
                return false;
            }

            return resolver.IsStepHeadAnchored(
                currentModule,
                currentStep,
                currentStep.GetString("anchorId", "anchor.head.default"));
        }

        private int IndexOfChoiceCard(GameObject hitObject)
        {
            if (hitObject == null)
            {
                return -1;
            }

            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                if (choiceVisuals[i].card == hitObject)
                {
                    return i;
                }
            }

            return -1;
        }

        private void EnsureBaseVisuals()
        {
            if (background == null)
            {
                background = SpatialRenderUtility.CreateQuad(
                    transform,
                    "MCQ Background",
                    SpatialRenderUtility.CreateTransparentColorMaterial(panelColor, nameof(SpatialMCQPanel)),
                    false,
                    SortingBackground,
                    ColliderDepth);
            }

            if (accentBar == null)
            {
                accentBar = SpatialRenderUtility.CreateQuad(
                    transform,
                    "MCQ Accent",
                    SpatialRenderUtility.CreateTransparentColorMaterial(accentColor, nameof(SpatialMCQPanel)),
                    false,
                    SortingAccent,
                    ColliderDepth);
            }

            if (titleText == null)
            {
                titleText = SpatialRenderUtility.CreateText(
                    transform,
                    "Title",
                    titleCharacterSize,
                    textFontSize,
                    titleColor,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            if (questionText == null)
            {
                questionText = SpatialRenderUtility.CreateText(
                    transform,
                    "Question",
                    questionCharacterSize,
                    textFontSize,
                    questionColor,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            if (feedbackText == null)
            {
                feedbackText = SpatialRenderUtility.CreateText(
                    transform,
                    "Feedback",
                    feedbackCharacterSize,
                    textFontSize,
                    feedbackColor,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            ApplyTextSettings();
            UpdateMaterialColors();
            UpdateVisualLayout();
        }

        private void RebuildChoiceVisuals()
        {
            ClearChoiceVisuals();

            for (int i = 0; i < choices.Length; i++)
            {
                GameObject card = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Choice " + (i + 1),
                    SpatialRenderUtility.CreateTransparentColorMaterial(choiceColor, nameof(SpatialMCQPanel)),
                    true,
                    SortingChoiceCard,
                    ColliderDepth);

                TextMesh text = SpatialRenderUtility.CreateText(
                    transform,
                    "Choice Text " + (i + 1),
                    choiceCharacterSize,
                    textFontSize,
                    choiceTextColor,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
                text.text = BuildChoiceText(i);

                ChoiceVisual visual = new ChoiceVisual();
                visual.card = card;
                visual.text = text;
                visual.renderer = card.GetComponent<Renderer>();
                visual.height = GetChoiceCardHeight(text.text);
                choiceVisuals.Add(visual);
            }

            UpdateVisualLayout();
            UpdateChoiceColors();
        }

        private string BuildChoiceText(int index)
        {
            string choice = choices != null && index >= 0 && index < choices.Length ? choices[index] ?? "" : "";
            string label = (index + 1) + ". " + choice;
            return SpatialRenderUtility.Wrap(label, GetEffectiveWrapCharacters(choiceCharacterSize, choiceTextInsetX * 2f));
        }

        private void ApplyTextSettings()
        {
            ConfigureText(titleText, titleCharacterSize, titleColor);
            ConfigureText(questionText, questionCharacterSize, questionColor);
            ConfigureText(feedbackText, feedbackCharacterSize, feedbackColor);

            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                ConfigureText(choiceVisuals[i].text, choiceCharacterSize, choiceTextColor);
            }
        }

        private void ConfigureText(TextMesh textMesh, float characterSize, Color color)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.anchor = TextAnchor.UpperLeft;
            textMesh.alignment = TextAlignment.Left;
            textMesh.fontSize = textFontSize;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
        }

        private void UpdateMaterialColors()
        {
            SpatialRenderUtility.SetRendererColor(background, panelColor);
            SpatialRenderUtility.SetRendererColor(accentBar, accentColor);
            UpdateChoiceColors();
        }

        private void UpdateVisualLayout()
        {
            if (background == null || titleText == null || questionText == null || feedbackText == null)
            {
                return;
            }

            string title = titleText.text ?? "";
            string question = questionText.text ?? "";
            string feedback = feedbackText.text ?? "";
            bool hasTitle = !string.IsNullOrWhiteSpace(title);
            bool hasQuestion = !string.IsNullOrWhiteSpace(question);
            bool hasFeedback = !string.IsNullOrWhiteSpace(feedback);

            Vector2 actualSize = autoSizePanel ? CalculatePanelSize(title, question, feedback, hasTitle, hasQuestion, hasFeedback) : panelSize;
            actualSize = new Vector2(Mathf.Max(0.1f, actualSize.x), Mathf.Max(0.1f, actualSize.y));

            background.transform.localPosition = new Vector3(0f, 0f, LocalZBackground);
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(actualSize.x, actualSize.y, 1f);

            float extraLeftInset = GetExtraLeftInset();
            float contentLeft = -actualSize.x * 0.5f + padding.x + extraLeftInset;
            float contentTop = actualSize.y * 0.5f - padding.y;
            float contentWidth = Mathf.Max(0.01f, actualSize.x - padding.x * 2f - extraLeftInset);
            float contentCenterX = contentLeft + contentWidth * 0.5f;
            float choiceWidth = contentWidth;

            if (accentBar != null)
            {
                accentBar.SetActive(showAccentBar);
                accentBar.transform.localPosition = new Vector3(
                    -actualSize.x * 0.5f + padding.x * 0.5f,
                    0f,
                    LocalZAccent);
                accentBar.transform.localRotation = Quaternion.identity;
                accentBar.transform.localScale = new Vector3(
                    accentWidth,
                    Mathf.Max(0.01f, actualSize.y - padding.y * 1.5f),
                    1f);
            }

            float cursorY = contentTop;
            titleText.transform.localPosition = new Vector3(contentLeft, cursorY, LocalZTitleQuestion);
            if (hasTitle)
            {
                cursorY -= SpatialRenderUtility.CountLines(title) * titleLineHeight;
            }

            if (hasTitle && hasQuestion)
            {
                cursorY -= titleQuestionGap;
            }

            questionText.transform.localPosition = new Vector3(contentLeft, cursorY, LocalZTitleQuestion);
            if (hasQuestion)
            {
                cursorY -= SpatialRenderUtility.CountLines(question) * questionLineHeight;
            }

            if ((hasTitle || hasQuestion) && choiceVisuals.Count > 0)
            {
                cursorY -= questionChoiceGap;
            }

            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                ChoiceVisual visual = choiceVisuals[i];
                float cardHeight = GetChoiceCardHeight(visual.text == null ? "" : visual.text.text);
                visual.height = cardHeight;

                if (i > 0)
                {
                    cursorY -= choiceGap;
                }

                float centerY = cursorY - cardHeight * 0.5f;

                if (visual.card != null)
                {
                    visual.card.transform.localPosition = new Vector3(contentCenterX, centerY, LocalZChoiceCard);
                    visual.card.transform.localRotation = Quaternion.identity;
                    visual.card.transform.localScale = new Vector3(choiceWidth, cardHeight, 1f);
                }

                if (visual.text != null)
                {
                    visual.text.transform.localPosition = new Vector3(
                        contentLeft + choiceTextInsetX,
                        centerY + cardHeight * 0.5f - choiceTextTopOffset,
                        LocalZChoiceText);
                }

                cursorY -= cardHeight;
            }

            if (hasFeedback)
            {
                if (choiceVisuals.Count > 0 || hasQuestion || hasTitle)
                {
                    cursorY -= feedbackGapBelowChoices;
                }
            }

            feedbackText.transform.localPosition = new Vector3(contentLeft, cursorY, LocalZFeedbackText);
        }

        private Vector2 CalculatePanelSize(
            string title,
            string question,
            string feedback,
            bool hasTitle,
            bool hasQuestion,
            bool hasFeedback)
        {
            float extraLeftInset = GetExtraLeftInset();
            float titleWidth = SpatialRenderUtility.LongestLineLength(title) * titleCharacterSize * estimatedCharacterWidth;
            float questionWidth = SpatialRenderUtility.LongestLineLength(question) * questionCharacterSize * estimatedCharacterWidth;
            float feedbackWidth = SpatialRenderUtility.LongestLineLength(feedback) * feedbackCharacterSize * estimatedCharacterWidth;
            float choiceWidth = GetDesiredChoicesWidth();

            float desiredContentWidth = Mathf.Max(
                Mathf.Max(titleWidth, questionWidth),
                Mathf.Max(choiceWidth, feedbackWidth));
            float desiredWidth = desiredContentWidth + padding.x * 2f + extraLeftInset;
            desiredWidth = Mathf.Clamp(desiredWidth, minimumPanelSize.x, maximumPanelSize.x);

            float desiredHeight = padding.y * 2f;
            if (hasTitle)
            {
                desiredHeight += SpatialRenderUtility.CountLines(title) * titleLineHeight;
            }

            if (hasTitle && hasQuestion)
            {
                desiredHeight += titleQuestionGap;
            }

            if (hasQuestion)
            {
                desiredHeight += SpatialRenderUtility.CountLines(question) * questionLineHeight;
            }

            if ((hasTitle || hasQuestion) && choiceVisuals.Count > 0)
            {
                desiredHeight += questionChoiceGap;
            }

            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                if (i > 0)
                {
                    desiredHeight += choiceGap;
                }

                desiredHeight += GetChoiceCardHeight(choiceVisuals[i].text == null ? "" : choiceVisuals[i].text.text);
            }

            if (hasFeedback)
            {
                if (choiceVisuals.Count > 0 || hasQuestion || hasTitle)
                {
                    desiredHeight += feedbackGapBelowChoices;
                }

                desiredHeight += SpatialRenderUtility.CountLines(feedback) * feedbackLineHeight;
            }

            if (!hasTitle && !hasQuestion && choiceVisuals.Count == 0 && !hasFeedback)
            {
                desiredHeight += questionLineHeight;
            }

            desiredHeight = Mathf.Clamp(desiredHeight, minimumPanelSize.y, maximumPanelSize.y);
            return new Vector2(desiredWidth, desiredHeight);
        }

        private float GetDesiredChoicesWidth()
        {
            float result = 0f;
            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                string text = choiceVisuals[i].text == null ? "" : choiceVisuals[i].text.text ?? "";
                float width = SpatialRenderUtility.LongestLineLength(text) * choiceCharacterSize * estimatedCharacterWidth + choiceTextInsetX * 2f;
                result = Mathf.Max(result, width);
            }

            return result;
        }

        private float GetChoiceCardHeight(string choiceText)
        {
            if (!autoSizeChoiceCards)
            {
                return choiceHeight;
            }

            int lineCount = Mathf.Max(1, SpatialRenderUtility.CountLines(choiceText));
            float desiredHeight = lineCount * choiceLineHeight + choiceVerticalPadding * 2f;
            return Mathf.Max(minimumChoiceHeight, desiredHeight);
        }

        private int GetEffectiveWrapCharacters(float characterSize, float extraHorizontalInset)
        {
            int result = Mathf.Max(1, wrapCharacters);
            if (!autoSizePanel)
            {
                return result;
            }

            float contentWidth = maximumPanelSize.x - padding.x * 2f - GetExtraLeftInset() - extraHorizontalInset;
            float averageCharacterWidth = Mathf.Max(0.001f, characterSize * estimatedCharacterWidth);
            int fitAtMaxWidth = Mathf.FloorToInt(contentWidth / averageCharacterWidth);

            if (fitAtMaxWidth > 0)
            {
                result = Mathf.Min(result, fitAtMaxWidth);
            }

            return Mathf.Max(12, result);
        }

        private float GetExtraLeftInset()
        {
            return showAccentBar ? accentWidth + accentGap : 0f;
        }

        private void ClearChoiceVisuals()
        {
            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                if (choiceVisuals[i].text != null)
                {
                    Destroy(choiceVisuals[i].text.gameObject);
                }

                if (choiceVisuals[i].card != null)
                {
                    Destroy(choiceVisuals[i].card);
                }
            }

            choiceVisuals.Clear();
        }

        private void UpdateChoiceColors()
        {
            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                Color color = choiceColor;

                if (selectedIndex >= 0)
                {
                    if (i == selectedIndex)
                    {
                        color = selectedIndex == correctIndex ? selectedColor : wrongColor;
                    }
                    else if (i == correctIndex)
                    {
                        color = selectedColor;
                    }
                }
                else if (i == gazedIndex)
                {
                    color = Color.Lerp(gazeColor, selectedColor, Mathf.Clamp01(gazeTimer / Mathf.Max(0.01f, gazeDwellSeconds)));
                }

                if (choiceVisuals[i].renderer != null && choiceVisuals[i].renderer.sharedMaterial != null)
                {
                    SpatialRenderUtility.SetMaterialColor(choiceVisuals[i].renderer.sharedMaterial, color);
                }
            }
        }

    }
}