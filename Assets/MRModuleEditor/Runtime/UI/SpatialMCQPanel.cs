using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using MRModuleEditor.Runtime.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialMCQPanel : MonoBehaviour
    {
        private class ChoiceVisual
        {
            public GameObject card;
            public TextMesh text;
            public Renderer renderer;
            public InteractableTarget target;
            public float height;
        }

        private const float LocalZBackground = 0f;
        private const float LocalZAccent = 0.005f;
        private const float LocalZChoiceCard = 0.01f;
        private const float ColliderDepth = 0.04f;

        private const int SortingBackground = 0;
        private const int SortingAccent = 1;
        private const int SortingChoiceCard = 1;
        private const int SortingText = 2;

        [SerializeField]
        private SpatialLayoutResolver spatialLayoutResolver;

        [Header("Style")]
        [SerializeField]
        private SpatialPanelStyle style;

        [Header("Interaction")]
        [SerializeField]
        private InteractionContext interactionContext;

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

        [Header("Input")]
        [SerializeField]
        private bool lockHeadAnchoredPanelForGaze = true;

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
        private InteractionContext subscribedInteractionContext;
        private int hoveredChoiceIndex = -1;
        private float hoverProgress;
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

        private SpatialPanelStyle Style
        {
            get { return style == null ? SpatialPanelStyle.Fallback : style; }
        }

        private SpatialPanelStyle.MCQPanelStyle MCQStyle
        {
            get { return Style.MCQPanel; }
        }

        private SpatialPanelStyle.PanelChromeStyle Chrome
        {
            get { return MCQStyle.chrome; }
        }

        private SpatialPanelStyle.ChoiceCardStyle ChoiceStyle
        {
            get { return Style.ChoiceCards; }
        }

        public bool HasAnswer
        {
            get { return selectedIndex >= 0; }
        }

        public int SelectedAnswer
        {
            get { return selectedIndex; }
        }

        private InteractionContext Interaction
        {
            get
            {
                if (interactionContext == null)
                {
                    interactionContext = FindFirstObjectByType<InteractionContext>(FindObjectsInactive.Include);
                }

                return interactionContext;
            }
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
            choiceHeight = Mathf.Max(0.02f, choiceHeight);
            minimumChoiceHeight = Mathf.Max(0.02f, minimumChoiceHeight);
            choiceVerticalPadding = Mathf.Max(0f, choiceVerticalPadding);
            choiceGap = Mathf.Max(0f, choiceGap);
            choiceTextInsetX = Mathf.Max(0f, choiceTextInsetX);
            choiceTextTopOffset = Mathf.Max(0f, choiceTextTopOffset);

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
            hoveredChoiceIndex = -1;
            hoverProgress = 0f;
            poseLockedForCurrentQuestion = false;
            hasAppliedPose = false;

            titleText.text = SpatialRenderUtility.Wrap(step == null ? "Quick Check" : step.title ?? "Quick Check", GetEffectiveWrapCharacters(MCQStyle.title.characterSize, 0f));
            questionText.text = SpatialRenderUtility.Wrap(question ?? "", GetEffectiveWrapCharacters(MCQStyle.question.characterSize, 0f));
            feedbackText.text = SpatialRenderUtility.Wrap(BuildInputInstruction(), GetEffectiveWrapCharacters(MCQStyle.feedback.characterSize, 0f));

            ApplyTextSettings();
            UpdateMaterialColors();
            RebuildChoiceVisuals();
            UpdateVisualLayout();
            gameObject.SetActive(true);
            SubscribeToInteractionContext();
            RegisterActiveChoiceTargets();

            bool poseApplied = ApplyAnchoredPose();
            poseLockedForCurrentQuestion = poseApplied
                && lockHeadAnchoredPanelForGaze
                && IsCurrentStepHeadAnchored();
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = SpatialRenderUtility.Wrap(message ?? "", GetEffectiveWrapCharacters(MCQStyle.feedback.characterSize, 0f));
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
            UnregisterActiveChoiceTargets();

            showingStepId = "";
            currentModule = null;
            currentStep = null;
            choices = new string[0];
            correctIndex = -1;
            selectedIndex = -1;
            hoveredChoiceIndex = -1;
            hoverProgress = 0f;
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
        }

        private void LateUpdate()
        {
            if (poseLockedForCurrentQuestion)
            {
                return;
            }

            bool poseApplied = ApplyAnchoredPose();
            if (poseApplied
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

            if (!Style.HeadFollow.smoothFollow || !Application.isPlaying || !hasAppliedPose)
            {
                transform.position = targetPose.position;
                transform.rotation = targetPose.rotation;
                hasAppliedPose = true;
                return;
            }

            float t = 1f - Mathf.Exp(-Style.HeadFollow.followSharpness * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPose.position) > Style.HeadFollow.snapDistance)
            {
                transform.position = targetPose.position;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPose.position, t);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetPose.rotation, t);
        }

        private string BuildInputInstruction()
        {

            return "Choose an answer.";
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

        private void EnsureBaseVisuals()
        {
            if (background == null)
            {
                background = SpatialRenderUtility.CreateQuad(
                    transform,
                    "MCQ Background",
                    SpatialRenderUtility.CreateTransparentColorMaterial(Chrome.panelColor, nameof(SpatialMCQPanel)),
                    false,
                    SortingBackground,
                    ColliderDepth);
            }

            if (accentBar == null)
            {
                accentBar = SpatialRenderUtility.CreateQuad(
                    transform,
                    "MCQ Accent",
                    SpatialRenderUtility.CreateTransparentColorMaterial(Style.AccentColor, nameof(SpatialMCQPanel)),
                    false,
                    SortingAccent,
                    ColliderDepth);
            }

            if (titleText == null)
            {
                titleText = SpatialRenderUtility.CreateText(
                    transform,
                    "Title",
                    MCQStyle.title.characterSize,
                    Style.TextFontSize,
                    MCQStyle.title.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            if (questionText == null)
            {
                questionText = SpatialRenderUtility.CreateText(
                    transform,
                    "Question",
                    MCQStyle.question.characterSize,
                    Style.TextFontSize,
                    MCQStyle.question.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            if (feedbackText == null)
            {
                feedbackText = SpatialRenderUtility.CreateText(
                    transform,
                    "Feedback",
                    MCQStyle.feedback.characterSize,
                    Style.TextFontSize,
                    MCQStyle.feedback.color,
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
                    SpatialRenderUtility.CreateTransparentColorMaterial(ChoiceStyle.choiceColor, nameof(SpatialMCQPanel)),
                    true,
                    SortingChoiceCard,
                    ColliderDepth);

                InteractableTarget target = card.GetComponent<InteractableTarget>();
                if (target == null)
                {
                    target = card.AddComponent<InteractableTarget>();
                }

                target.Configure(
                    showingStepId,
                    BuildChoiceTargetId(showingStepId, i),
                    i);

                TextMesh text = SpatialRenderUtility.CreateText(
                    transform,
                    "Choice Text " + (i + 1),
                    MCQStyle.choice.characterSize,
                    Style.TextFontSize,
                    MCQStyle.choice.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
                text.text = BuildChoiceText(i);

                ChoiceVisual visual = new ChoiceVisual();
                visual.card = card;
                visual.text = text;
                visual.renderer = card.GetComponent<Renderer>();
                visual.target = target;
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
            return SpatialRenderUtility.Wrap(label, GetEffectiveWrapCharacters(MCQStyle.choice.characterSize, choiceTextInsetX * 2f));
        }

        private void ApplyTextSettings()
        {
            ConfigureText(titleText, MCQStyle.title.characterSize, MCQStyle.title.color);
            ConfigureText(questionText, MCQStyle.question.characterSize, MCQStyle.question.color);
            ConfigureText(feedbackText, MCQStyle.feedback.characterSize, MCQStyle.feedback.color);

            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                ConfigureText(choiceVisuals[i].text, MCQStyle.choice.characterSize, MCQStyle.choice.color);
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
            textMesh.fontSize = Style.TextFontSize;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
        }

        private void UpdateMaterialColors()
        {
            SpatialRenderUtility.SetRendererColor(background, Chrome.panelColor);
            SpatialRenderUtility.SetRendererColor(accentBar, Style.AccentColor);
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
            float contentLeft = -actualSize.x * 0.5f + Chrome.padding.x + extraLeftInset;
            float contentTop = actualSize.y * 0.5f - Chrome.padding.y;
            float contentWidth = Mathf.Max(0.01f, actualSize.x - Chrome.padding.x * 2f - extraLeftInset);
            float contentCenterX = contentLeft + contentWidth * 0.5f;
            float choiceWidth = contentWidth;

            if (accentBar != null)
            {
                accentBar.SetActive(Chrome.showAccentBar);
                accentBar.transform.localPosition = new Vector3(
                    -actualSize.x * 0.5f + Chrome.padding.x * 0.5f,
                    0f,
                    LocalZAccent);
                accentBar.transform.localRotation = Quaternion.identity;
                accentBar.transform.localScale = new Vector3(
                    Style.AccentWidth,
                    Mathf.Max(0.01f, actualSize.y - Chrome.padding.y * 1.5f),
                    1f);
            }

            float cursorY = contentTop;
            titleText.transform.localPosition = new Vector3(contentLeft, cursorY, Chrome.textDepthOffset);
            if (hasTitle)
            {
                cursorY -= SpatialRenderUtility.CountLines(title) * MCQStyle.title.lineHeight;
            }

            if (hasTitle && hasQuestion)
            {
                cursorY -= MCQStyle.titleQuestionGap;
            }

            questionText.transform.localPosition = new Vector3(contentLeft, cursorY, Chrome.textDepthOffset);
            if (hasQuestion)
            {
                cursorY -= SpatialRenderUtility.CountLines(question) * MCQStyle.question.lineHeight;
            }

            if ((hasTitle || hasQuestion) && choiceVisuals.Count > 0)
            {
                cursorY -= MCQStyle.questionChoiceGap;
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
                        Chrome.textDepthOffset + 0.01f);
                }

                cursorY -= cardHeight;
            }

            if (hasFeedback)
            {
                if (choiceVisuals.Count > 0 || hasQuestion || hasTitle)
                {
                    cursorY -= MCQStyle.feedbackGapBelowChoices;
                }
            }

            feedbackText.transform.localPosition = new Vector3(contentLeft, cursorY, Chrome.textDepthOffset + 0.01f);
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
            float titleWidth = SpatialRenderUtility.LongestLineLength(title) * MCQStyle.title.characterSize * MCQStyle.estimatedCharacterWidth;
            float questionWidth = SpatialRenderUtility.LongestLineLength(question) * MCQStyle.question.characterSize * MCQStyle.estimatedCharacterWidth;
            float feedbackWidth = SpatialRenderUtility.LongestLineLength(feedback) * MCQStyle.feedback.characterSize * MCQStyle.estimatedCharacterWidth;
            float choiceWidth = GetDesiredChoicesWidth();

            float desiredContentWidth = Mathf.Max(
                Mathf.Max(titleWidth, questionWidth),
                Mathf.Max(choiceWidth, feedbackWidth));
            float desiredWidth = desiredContentWidth + Chrome.padding.x * 2f + extraLeftInset;
            desiredWidth = Mathf.Clamp(desiredWidth, minimumPanelSize.x, maximumPanelSize.x);

            float desiredHeight = Chrome.padding.y * 2f;
            if (hasTitle)
            {
                desiredHeight += SpatialRenderUtility.CountLines(title) * MCQStyle.title.lineHeight;
            }

            if (hasTitle && hasQuestion)
            {
                desiredHeight += MCQStyle.titleQuestionGap;
            }

            if (hasQuestion)
            {
                desiredHeight += SpatialRenderUtility.CountLines(question) * MCQStyle.question.lineHeight;
            }

            if ((hasTitle || hasQuestion) && choiceVisuals.Count > 0)
            {
                desiredHeight += MCQStyle.questionChoiceGap;
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
                    desiredHeight += MCQStyle.feedbackGapBelowChoices;
                }

                desiredHeight += SpatialRenderUtility.CountLines(feedback) * MCQStyle.feedback.lineHeight;
            }

            if (!hasTitle && !hasQuestion && choiceVisuals.Count == 0 && !hasFeedback)
            {
                desiredHeight += MCQStyle.question.lineHeight;
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
                float width = SpatialRenderUtility.LongestLineLength(text) * MCQStyle.choice.characterSize * MCQStyle.estimatedCharacterWidth + choiceTextInsetX * 2f;
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
            float desiredHeight = lineCount * MCQStyle.choice.lineHeight + choiceVerticalPadding * 2f;
            return Mathf.Max(minimumChoiceHeight, desiredHeight);
        }

        private int GetEffectiveWrapCharacters(float characterSize, float extraHorizontalInset)
        {
            int result = Mathf.Max(1, Style.WrapCharacters);
            if (!autoSizePanel)
            {
                return result;
            }

            float contentWidth = maximumPanelSize.x - Chrome.padding.x * 2f - GetExtraLeftInset() - extraHorizontalInset;
            float averageCharacterWidth = Mathf.Max(0.001f, characterSize * MCQStyle.estimatedCharacterWidth);
            int fitAtMaxWidth = Mathf.FloorToInt(contentWidth / averageCharacterWidth);

            if (fitAtMaxWidth > 0)
            {
                result = Mathf.Min(result, fitAtMaxWidth);
            }

            return Mathf.Max(12, result);
        }

        private float GetExtraLeftInset()
        {
            return Chrome.showAccentBar ? Style.AccentWidth + Chrome.accentGap : 0f;
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
                Color color = ChoiceStyle.choiceColor;

                if (selectedIndex >= 0)
                {
                    if (i == selectedIndex)
                    {
                        color = selectedIndex == correctIndex ? ChoiceStyle.selectedColor : ChoiceStyle.wrongColor;
                    }
                    else if (i == correctIndex)
                    {
                        color = ChoiceStyle.selectedColor;
                    }
                }
                else if (i == hoveredChoiceIndex)
                {
                    color = Color.Lerp(ChoiceStyle.gazeColor, ChoiceStyle.selectedColor, Mathf.Clamp01(hoverProgress));
                }

                if (choiceVisuals[i].renderer != null && choiceVisuals[i].renderer.sharedMaterial != null)
                {
                    SpatialRenderUtility.SetMaterialColor(choiceVisuals[i].renderer.sharedMaterial, color);
                }
            }
        }

        private static string BuildChoiceTargetId(string stepId, int choiceIndex)
        {
            return (stepId ?? "") + ".choice." + choiceIndex;
        }

        private bool IsSignalForCurrentChoice(InteractionSignal signal)
        {
            if (string.IsNullOrWhiteSpace(showingStepId))
            {
                return false;
            }

            if (signal.intPayload < 0 || signal.intPayload >= choices.Length)
            {
                return false;
            }

            return signal.targetId == BuildChoiceTargetId(showingStepId, signal.intPayload);
        }

        private void SubscribeToInteractionContext()
        {
            InteractionContext context = Interaction;
            if (subscribedInteractionContext == context)
            {
                return;
            }

            if (subscribedInteractionContext != null)
            {
                subscribedInteractionContext.SignalEmitted -= HandleInteractionSignal;
            }

            subscribedInteractionContext = context;

            if (subscribedInteractionContext != null)
            {
                subscribedInteractionContext.SignalEmitted += HandleInteractionSignal;
            }
        }

        private void OnDestroy()
        {
            UnregisterActiveChoiceTargets();

            if (subscribedInteractionContext != null)
            {
                subscribedInteractionContext.SignalEmitted -= HandleInteractionSignal;
                subscribedInteractionContext = null;
            }
        }

        private void HandleInteractionSignal(InteractionSignal signal)
        {
            if (!gameObject.activeInHierarchy || selectedIndex >= 0)
            {
                return;
            }

            if (!IsSignalForCurrentChoice(signal))
            {
                return;
            }

            if (signal.action == InteractionAction.Select)
            {
                SubmitAnswer(signal.intPayload);
                return;
            }

            if (signal.action == InteractionAction.HoverEnter)
            {
                hoveredChoiceIndex = signal.intPayload;
                hoverProgress = 0f;
                UpdateChoiceColors();
                return;
            }

            if (signal.action == InteractionAction.HoverProgress)
            {
                hoveredChoiceIndex = signal.intPayload;
                hoverProgress = Mathf.Clamp01(signal.floatPayload);
                UpdateChoiceColors();
                return;
            }

            if (signal.action == InteractionAction.HoverExit && hoveredChoiceIndex == signal.intPayload)
            {
                hoveredChoiceIndex = -1;
                hoverProgress = 0f;
                UpdateChoiceColors();
            }
        }

        private void RegisterActiveChoiceTargets()
        {
            InteractionContext context = Interaction;
            if (context == null || string.IsNullOrWhiteSpace(showingStepId))
            {
                return;
            }

            context.ClearTargetsForGroup(showingStepId);

            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                if (choiceVisuals[i].target != null)
                {
                    context.RegisterTarget(choiceVisuals[i].target);
                }
            }
        }

        private void UnregisterActiveChoiceTargets()
        {
            InteractionContext context = Interaction;
            if (context == null || string.IsNullOrWhiteSpace(showingStepId))
            {
                return;
            }

            context.ClearTargetsForGroup(showingStepId);
        }
    }
}