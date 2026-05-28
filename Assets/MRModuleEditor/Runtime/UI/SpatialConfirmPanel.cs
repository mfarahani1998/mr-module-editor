using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using MRModuleEditor.Runtime.Interaction;
using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialConfirmPanel : MonoBehaviour
    {
        private const float LocalZBackground = 0f;
        private const float LocalZAccent = 0.005f;
        private const float LocalZButton = 0.01f;
        private const float ColliderDepth = 0.04f;

        private const int SortingBackground = 0;
        private const int SortingAccent = 1;
        private const int SortingButton = 1;
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
        private Vector2 panelSize = new Vector2(1.65f, 1.2f);

        [SerializeField]
        private Vector3 panelLocalOffset = new Vector3(0f, 0f, 0f);

        [SerializeField]
        private bool applyPanelLocalOffsetToAuthoredLayouts = false;

        [SerializeField]
        private Vector2 minimumPanelSize = new Vector2(0.95f, 0.48f);

        [SerializeField]
        private Vector2 maximumPanelSize = new Vector2(1.95f, 1.45f);

        [Header("Button")]
        [SerializeField]
        private bool autoSizeButton = true;

        // Used as the fixed size when autoSizeButton is disabled.
        [SerializeField]
        private Vector2 buttonSize = new Vector2(0.95f, 0.22f);

        [SerializeField]
        private Vector2 minimumButtonSize = new Vector2(0.46f, 0.18f);

        [SerializeField]
        private float maximumButtonWidth = 1.25f;

        // Used as a lower bound for the button position when autoSizePanel is disabled.
        [SerializeField]
        private float buttonBottomInset = 0.12f;

        [Header("Input")]
        [SerializeField]
        private bool lockHeadAnchoredPanelForGaze = true;

        private GameObject background;
        private GameObject accentBar;
        private GameObject buttonCard;
        private TextMesh titleText;
        private TextMesh bodyText;
        private TextMesh buttonText;
        private Renderer buttonRenderer;
        private InteractableTarget confirmTarget;

        private ModuleDocument currentModule;
        private ModuleStep currentStep;
        private string showingStepId = "";
        private bool confirmationReceived;
        private InteractionContext subscribedInteractionContext;
        private bool hoverActive;
        private float hoverProgress;
        private bool poseLocked;
        private bool hasAppliedPose;

        public bool HasConfirmation
        {
            get { return confirmationReceived; }
        }

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

        private SpatialPanelStyle.ConfirmPanelStyle ConfirmStyle
        {
            get { return Style.ConfirmPanel; }
        }

        private SpatialPanelStyle.PanelChromeStyle Chrome
        {
            get { return ConfirmStyle.chrome; }
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
            EnsureVisuals();
            Clear();
        }

        private void OnValidate()
        {
            panelSize = SpatialRenderUtility.ClampVector(panelSize, new Vector2(0.1f, 0.1f));
            minimumPanelSize = SpatialRenderUtility.ClampVector(minimumPanelSize, new Vector2(0.1f, 0.1f));
            maximumPanelSize = new Vector2(
                Mathf.Max(maximumPanelSize.x, minimumPanelSize.x),
                Mathf.Max(maximumPanelSize.y, minimumPanelSize.y));

            buttonSize = SpatialRenderUtility.ClampVector(buttonSize, new Vector2(0.01f, 0.01f));
            minimumButtonSize = SpatialRenderUtility.ClampVector(minimumButtonSize, new Vector2(0.01f, 0.01f));
            maximumButtonWidth = Mathf.Max(minimumButtonSize.x, maximumButtonWidth);
            buttonBottomInset = Mathf.Max(0f, buttonBottomInset);

            if (background != null)
            {
                ApplyTextSettings();
                UpdateMaterialColors();
                UpdateVisualLayout();
            }
        }

        public void ShowConfirm(ModuleDocument module, ModuleStep step, string message, string buttonLabel)
        {
            EnsureVisuals();

            currentModule = module;
            currentStep = step;
            showingStepId = step == null ? "" : step.id;
            confirmationReceived = false;
            hoverActive = false;
            hoverProgress = 0f;
            hasAppliedPose = false;
            poseLocked = false;

            string resolvedTitle = ResolveTitle(step);
            string resolvedButtonLabel = string.IsNullOrWhiteSpace(buttonLabel) ? "Continue" : buttonLabel;

            titleText.text = SpatialRenderUtility.Wrap(resolvedTitle, GetEffectiveWrapCharacters(ConfirmStyle.title.characterSize, 0f));
            bodyText.text = SpatialRenderUtility.Wrap(message ?? "", GetEffectiveWrapCharacters(ConfirmStyle.body.characterSize, 0f));
            buttonText.text = SpatialRenderUtility.Wrap(resolvedButtonLabel, GetEffectiveButtonWrapCharacters());

            if (confirmTarget != null)
            {
                confirmTarget.Configure(showingStepId, BuildConfirmTargetId(showingStepId), 0);
            }

            ApplyTextSettings();
            UpdateMaterialColors();
            UpdateVisualLayout();
            gameObject.SetActive(true);
            SubscribeToInteractionContext();
            RegisterActiveTarget();

            bool poseApplied = ApplyAnchoredPose();
            poseLocked = poseApplied
                && lockHeadAnchoredPanelForGaze
                && IsCurrentStepHeadAnchored();
        }

        public void SubmitConfirmation()
        {
            if (confirmationReceived)
            {
                return;
            }

            confirmationReceived = true;
            hoverActive = false;
            hoverProgress = 1f;
            UpdateMaterialColors();
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
            UnregisterActiveTarget();

            showingStepId = "";
            currentModule = null;
            currentStep = null;
            confirmationReceived = false;
            hoverActive = false;
            hoverProgress = 0f;
            hasAppliedPose = false;
            poseLocked = false;

            if (titleText != null) titleText.text = "";
            if (bodyText != null) bodyText.text = "";
            if (buttonText != null) buttonText.text = "";
            if (background != null) UpdateVisualLayout();

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (poseLocked)
            {
                return;
            }

            bool poseApplied = ApplyAnchoredPose();
            if (poseApplied
                && lockHeadAnchoredPanelForGaze
                && IsCurrentStepHeadAnchored())
            {
                poseLocked = true;
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

        private void EnsureVisuals()
        {
            if (background == null)
            {
                background = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Confirm Background",
                    SpatialRenderUtility.CreateTransparentColorMaterial(Chrome.panelColor, nameof(SpatialConfirmPanel)),
                    false,
                    SortingBackground,
                    ColliderDepth);
            }

            if (accentBar == null)
            {
                accentBar = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Confirm Accent",
                    SpatialRenderUtility.CreateTransparentColorMaterial(Style.AccentColor, nameof(SpatialConfirmPanel)),
                    false,
                    SortingAccent,
                    ColliderDepth);
            }

            if (buttonCard == null)
            {
                buttonCard = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Confirm Button",
                    SpatialRenderUtility.CreateTransparentColorMaterial(ConfirmStyle.buttonColor, nameof(SpatialConfirmPanel)),
                    true,
                    SortingButton,
                    ColliderDepth);
            }

            if (buttonRenderer == null && buttonCard != null)
            {
                buttonRenderer = buttonCard.GetComponent<Renderer>();
            }

            if (confirmTarget == null && buttonCard != null)
            {
                confirmTarget = buttonCard.GetComponent<InteractableTarget>();
                if (confirmTarget == null)
                {
                    confirmTarget = buttonCard.AddComponent<InteractableTarget>();
                }
            }

            if (titleText == null)
            {
                titleText = SpatialRenderUtility.CreateText(
                    transform,
                    "Confirm Title",
                    ConfirmStyle.title.characterSize,
                    Style.TextFontSize,
                    ConfirmStyle.title.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            if (bodyText == null)
            {
                bodyText = SpatialRenderUtility.CreateText(
                    transform,
                    "Confirm Body",
                    ConfirmStyle.body.characterSize,
                    Style.TextFontSize,
                    ConfirmStyle.body.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            if (buttonText == null)
            {
                buttonText = SpatialRenderUtility.CreateText(
                    transform,
                    "Confirm Button Text",
                    ConfirmStyle.button.characterSize,
                    Style.TextFontSize,
                    ConfirmStyle.button.color,
                    TextAnchor.MiddleCenter,
                    TextAlignment.Center,
                    SortingText + 1);
            }

            ApplyTextSettings();
            UpdateMaterialColors();
            UpdateVisualLayout();
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

        private void ApplyTextSettings()
        {
            ConfigureText(titleText, ConfirmStyle.title, TextAnchor.UpperLeft, TextAlignment.Left);
            ConfigureText(bodyText, ConfirmStyle.body, TextAnchor.UpperLeft, TextAlignment.Left);
            ConfigureText(buttonText, ConfirmStyle.button, TextAnchor.MiddleCenter, TextAlignment.Center);
        }

        private void ConfigureText(TextMesh textMesh, SpatialPanelStyle.TextRoleStyle textStyle, TextAnchor anchor, TextAlignment alignment)
        {
            if (textMesh == null || textStyle == null)
            {
                return;
            }

            textMesh.fontSize = Style.TextFontSize;
            textMesh.characterSize = Mathf.Max(0.001f, textStyle.characterSize);
            textMesh.color = textStyle.color;
            textMesh.anchor = anchor;
            textMesh.alignment = alignment;
        }

        private void UpdateMaterialColors()
        {
            SpatialRenderUtility.SetRendererColor(background, Chrome.panelColor);
            SpatialRenderUtility.SetRendererColor(accentBar, Style.AccentColor);

            Color buttonColor = ConfirmStyle.buttonColor;
            if (confirmationReceived)
            {
                buttonColor = ConfirmStyle.buttonConfirmedColor;
            }
            else if (hoverActive)
            {
                buttonColor = Color.Lerp(ConfirmStyle.buttonGazeColor, ConfirmStyle.buttonConfirmedColor, Mathf.Clamp01(hoverProgress));
            }

            if (buttonRenderer != null && buttonRenderer.sharedMaterial != null)
            {
                SpatialRenderUtility.SetMaterialColor(buttonRenderer.sharedMaterial, buttonColor);
            }
        }

        private void UpdateVisualLayout()
        {
            if (background == null || titleText == null || bodyText == null || buttonCard == null || buttonText == null)
            {
                return;
            }

            string title = titleText.text ?? "";
            string body = bodyText.text ?? "";
            string button = buttonText.text ?? "";
            bool hasTitle = !string.IsNullOrWhiteSpace(title);
            bool hasBody = !string.IsNullOrWhiteSpace(body);
            bool hasButton = !string.IsNullOrWhiteSpace(button);

            Vector2 actualSize = autoSizePanel ? CalculatePanelSize(title, body, button, hasTitle, hasBody, hasButton) : panelSize;
            actualSize = new Vector2(Mathf.Max(0.1f, actualSize.x), Mathf.Max(0.1f, actualSize.y));

            background.transform.localPosition = new Vector3(0f, 0f, LocalZBackground);
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(actualSize.x, actualSize.y, 1f);

            float extraLeftInset = GetExtraLeftInset();
            float contentLeft = -actualSize.x * 0.5f + Chrome.padding.x + extraLeftInset;
            float contentTop = actualSize.y * 0.5f - Chrome.padding.y;
            float contentWidth = Mathf.Max(0.01f, actualSize.x - Chrome.padding.x * 2f - extraLeftInset);
            float contentCenterX = contentLeft + contentWidth * 0.5f;

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

            titleText.gameObject.SetActive(hasTitle);
            bodyText.gameObject.SetActive(hasBody);
            buttonCard.SetActive(hasButton);
            buttonText.gameObject.SetActive(hasButton);

            float cursorY = contentTop;
            titleText.transform.localPosition = new Vector3(contentLeft, cursorY, Chrome.textDepthOffset);
            if (hasTitle)
            {
                cursorY -= SpatialRenderUtility.CountLines(title) * ConfirmStyle.title.lineHeight;
            }

            if (hasTitle && hasBody)
            {
                cursorY -= ConfirmStyle.titleBodyGap;
            }

            bodyText.transform.localPosition = new Vector3(contentLeft, cursorY, Chrome.textDepthOffset);
            if (hasBody)
            {
                cursorY -= SpatialRenderUtility.CountLines(body) * ConfirmStyle.body.lineHeight;
            }

            if ((hasTitle || hasBody) && hasButton)
            {
                cursorY -= ConfirmStyle.bodyButtonGap;
            }

            if (hasButton)
            {
                Vector2 actualButtonSize = CalculateButtonSize(button, contentWidth);
                float buttonCenterY = cursorY - actualButtonSize.y * 0.5f;

                if (!autoSizePanel)
                {
                    float bottomButtonCenterY = -actualSize.y * 0.5f + buttonBottomInset + actualButtonSize.y * 0.5f;
                    buttonCenterY = Mathf.Max(buttonCenterY, bottomButtonCenterY);
                }

                buttonCard.transform.localPosition = new Vector3(contentCenterX, buttonCenterY, LocalZButton);
                buttonCard.transform.localRotation = Quaternion.identity;
                buttonCard.transform.localScale = new Vector3(actualButtonSize.x, actualButtonSize.y, 1f);

                buttonText.transform.localPosition = new Vector3(
                    contentCenterX,
                    buttonCenterY + ConfirmStyle.buttonTextVerticalOffset,
                    Chrome.textDepthOffset + 0.01f);
                buttonText.transform.localRotation = Quaternion.identity;
            }
        }

        private Vector2 CalculatePanelSize(string title, string body, string button, bool hasTitle, bool hasBody, bool hasButton)
        {
            float extraLeftInset = GetExtraLeftInset();
            float availableContentWidth = Mathf.Max(0.01f, maximumPanelSize.x - Chrome.padding.x * 2f - extraLeftInset);

            float titleWidth = SpatialRenderUtility.LongestLineLength(title) * ConfirmStyle.title.characterSize * ConfirmStyle.estimatedCharacterWidth;
            float bodyWidth = SpatialRenderUtility.LongestLineLength(body) * ConfirmStyle.body.characterSize * ConfirmStyle.estimatedCharacterWidth;
            Vector2 preferredButtonSize = hasButton ? CalculateButtonSize(button, availableContentWidth) : Vector2.zero;

            float desiredContentWidth = Mathf.Max(Mathf.Max(titleWidth, bodyWidth), preferredButtonSize.x);
            float desiredWidth = desiredContentWidth + Chrome.padding.x * 2f + extraLeftInset;
            desiredWidth = Mathf.Clamp(desiredWidth, minimumPanelSize.x, maximumPanelSize.x);

            float desiredHeight = Chrome.padding.y * 2f;
            if (hasTitle)
            {
                desiredHeight += SpatialRenderUtility.CountLines(title) * ConfirmStyle.title.lineHeight;
            }

            if (hasTitle && hasBody)
            {
                desiredHeight += ConfirmStyle.titleBodyGap;
            }

            if (hasBody)
            {
                desiredHeight += SpatialRenderUtility.CountLines(body) * ConfirmStyle.body.lineHeight;
            }

            if ((hasTitle || hasBody) && hasButton)
            {
                desiredHeight += ConfirmStyle.bodyButtonGap;
            }

            if (hasButton)
            {
                desiredHeight += preferredButtonSize.y;
            }

            if (!hasTitle && !hasBody && !hasButton)
            {
                desiredHeight += ConfirmStyle.body.lineHeight;
            }

            desiredHeight = Mathf.Clamp(desiredHeight, minimumPanelSize.y, maximumPanelSize.y);
            return new Vector2(desiredWidth, desiredHeight);
        }

        private Vector2 CalculateButtonSize(string button, float availableContentWidth)
        {
            float contentWidth = Mathf.Max(0.01f, availableContentWidth);

            if (!autoSizeButton)
            {
                return new Vector2(
                    Mathf.Max(0.01f, Mathf.Min(buttonSize.x, contentWidth)),
                    Mathf.Max(0.01f, buttonSize.y));
            }

            float minimumWidth = Mathf.Min(Mathf.Max(0.01f, minimumButtonSize.x), contentWidth);
            float maximumWidth = Mathf.Max(minimumWidth, Mathf.Min(Mathf.Max(minimumWidth, maximumButtonWidth), contentWidth));
            float textWidth = SpatialRenderUtility.LongestLineLength(button) * ConfirmStyle.button.characterSize * ConfirmStyle.estimatedCharacterWidth;
            float desiredWidth = textWidth + ConfirmStyle.buttonHorizontalPadding * 2f;
            desiredWidth = Mathf.Clamp(desiredWidth, minimumWidth, maximumWidth);

            int lineCount = Mathf.Max(1, SpatialRenderUtility.CountLines(button));
            float desiredHeight = lineCount * ConfirmStyle.button.lineHeight + ConfirmStyle.buttonVerticalPadding * 2f;
            desiredHeight = Mathf.Max(minimumButtonSize.y, desiredHeight);
            return new Vector2(desiredWidth, desiredHeight);
        }

        private int GetEffectiveWrapCharacters(float characterSize, float extraHorizontalInset)
        {
            int result = Mathf.Max(1, Style.WrapCharacters);
            float panelWidth = autoSizePanel ? maximumPanelSize.x : panelSize.x;
            float contentWidth = panelWidth - Chrome.padding.x * 2f - GetExtraLeftInset() - extraHorizontalInset;
            float averageCharacterWidth = Mathf.Max(0.001f, characterSize * ConfirmStyle.estimatedCharacterWidth);
            int fit = Mathf.FloorToInt(contentWidth / averageCharacterWidth);

            if (fit > 0)
            {
                result = Mathf.Min(result, fit);
            }

            return Mathf.Max(12, result);
        }

        private int GetEffectiveButtonWrapCharacters()
        {
            int result = Mathf.Max(1, Style.WrapCharacters);
            float panelWidth = autoSizePanel ? maximumPanelSize.x : panelSize.x;
            float contentWidth = Mathf.Max(0.01f, panelWidth - Chrome.padding.x * 2f - GetExtraLeftInset());
            float availableButtonWidth = autoSizeButton
                ? Mathf.Min(maximumButtonWidth, contentWidth)
                : Mathf.Min(buttonSize.x, contentWidth);
            float textWidth = Mathf.Max(0.01f, availableButtonWidth - ConfirmStyle.buttonHorizontalPadding * 2f);
            float averageCharacterWidth = Mathf.Max(0.001f, ConfirmStyle.button.characterSize * ConfirmStyle.estimatedCharacterWidth);
            int fit = Mathf.FloorToInt(textWidth / averageCharacterWidth);

            if (fit > 0)
            {
                result = Mathf.Min(result, fit);
            }

            return Mathf.Max(8, result);
        }

        private float GetExtraLeftInset()
        {
            return Chrome.showAccentBar ? Style.AccentWidth + Chrome.accentGap : 0f;
        }

        private static string ResolveTitle(ModuleStep step)
        {
            if (step == null || string.IsNullOrWhiteSpace(step.title))
            {
                return "Confirm";
            }

            return step.title;
        }

        private static string BuildConfirmTargetId(string stepId)
        {
            return (stepId ?? "") + ".confirm";
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

        private void HandleInteractionSignal(InteractionSignal signal)
        {
            if (!gameObject.activeInHierarchy || confirmationReceived)
            {
                return;
            }

            if (signal.targetId != BuildConfirmTargetId(showingStepId))
            {
                return;
            }

            if (signal.action == InteractionAction.Select)
            {
                SubmitConfirmation();
                return;
            }

            if (signal.action == InteractionAction.HoverEnter)
            {
                hoverActive = true;
                hoverProgress = 0f;
                UpdateMaterialColors();
                return;
            }

            if (signal.action == InteractionAction.HoverProgress)
            {
                hoverActive = true;
                hoverProgress = Mathf.Clamp01(signal.floatPayload);
                UpdateMaterialColors();
                return;
            }

            if (signal.action == InteractionAction.HoverExit)
            {
                hoverActive = false;
                hoverProgress = 0f;
                UpdateMaterialColors();
            }
        }

        private void RegisterActiveTarget()
        {
            InteractionContext context = Interaction;
            if (context == null || confirmTarget == null || string.IsNullOrWhiteSpace(showingStepId))
            {
                return;
            }

            context.ClearTargetsForGroup(showingStepId);
            context.RegisterTarget(confirmTarget);
        }

        private void UnregisterActiveTarget()
        {
            InteractionContext context = Interaction;
            if (context == null || string.IsNullOrWhiteSpace(showingStepId))
            {
                return;
            }

            context.ClearTargetsForGroup(showingStepId);
        }

        private void OnDestroy()
        {
            UnregisterActiveTarget();

            if (subscribedInteractionContext != null)
            {
                subscribedInteractionContext.SignalEmitted -= HandleInteractionSignal;
                subscribedInteractionContext = null;
            }
        }
    }
}
