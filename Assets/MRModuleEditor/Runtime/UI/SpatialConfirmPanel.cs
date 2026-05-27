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
        private const float LocalZText = 0.02f;
        private const float ColliderDepth = 0.04f;

        [SerializeField]
        private SpatialLayoutResolver spatialLayoutResolver;

        [SerializeField]
        private SpatialPanelStyle style;

        [SerializeField]
        private InteractionContext interactionContext;

        [Header("Panel")]
        [SerializeField]
        private Vector2 panelSize = new Vector2(1.65f, 1.2f);

        [SerializeField]
        private Vector3 panelLocalOffset = new Vector3(0f, 0f, 0f);

        [SerializeField]
        private bool applyPanelLocalOffsetToAuthoredLayouts = false;

        [Header("Button")]
        [SerializeField]
        private Vector2 buttonSize = new Vector2(0.95f, 0.22f);

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

        private SpatialPanelStyle.TextPanelStyle TextStyle
        {
            get { return Style.TextPanel; }
        }

        private SpatialPanelStyle.PanelChromeStyle Chrome
        {
            get { return TextStyle.chrome; }
        }

        private SpatialPanelStyle.ChoiceCardStyle ChoiceStyle
        {
            get { return Style.ChoiceCards; }
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
            panelSize = SpatialRenderUtility.ClampVector(panelSize, new Vector2(0.25f, 0.25f));
            buttonSize = SpatialRenderUtility.ClampVector(buttonSize, new Vector2(0.1f, 0.08f));
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

            titleText.text = SpatialRenderUtility.Wrap(step == null ? "Confirm" : step.title ?? "Confirm", GetEffectiveWrapCharacters(TextStyle.title.characterSize));
            bodyText.text = SpatialRenderUtility.Wrap(message ?? "", GetEffectiveWrapCharacters(TextStyle.body.characterSize));
            buttonText.text = SpatialRenderUtility.Wrap(string.IsNullOrWhiteSpace(buttonLabel) ? "Continue" : buttonLabel, GetEffectiveWrapCharacters(TextStyle.body.characterSize));

            confirmTarget.Configure(showingStepId, BuildConfirmTargetId(showingStepId), 0);

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

            transform.localScale = targetScale;

            if (!Style.HeadFollow.smoothFollow || !Application.isPlaying || !hasAppliedPose)
            {
                transform.position = targetPose.position;
                transform.rotation = targetPose.rotation;
                hasAppliedPose = true;
                return true;
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
            return true;
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
                    0,
                    ColliderDepth);
            }

            if (accentBar == null)
            {
                accentBar = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Confirm Accent",
                    SpatialRenderUtility.CreateTransparentColorMaterial(Style.AccentColor, nameof(SpatialConfirmPanel)),
                    false,
                    1,
                    ColliderDepth);
            }

            if (buttonCard == null)
            {
                buttonCard = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Confirm Button",
                    SpatialRenderUtility.CreateTransparentColorMaterial(ChoiceStyle.choiceColor, nameof(SpatialConfirmPanel)),
                    true,
                    2,
                    ColliderDepth);
                buttonRenderer = buttonCard.GetComponent<Renderer>();
                confirmTarget = buttonCard.AddComponent<InteractableTarget>();
            }

            if (titleText == null)
            {
                titleText = SpatialRenderUtility.CreateText(
                    transform,
                    "Confirm Title",
                    TextStyle.title.characterSize,
                    Style.TextFontSize,
                    TextStyle.title.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    3);
            }

            if (bodyText == null)
            {
                bodyText = SpatialRenderUtility.CreateText(
                    transform,
                    "Confirm Body",
                    TextStyle.body.characterSize,
                    Style.TextFontSize,
                    TextStyle.body.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    3);
            }

            if (buttonText == null)
            {
                buttonText = SpatialRenderUtility.CreateText(
                    transform,
                    "Confirm Button Text",
                    TextStyle.body.characterSize,
                    Style.TextFontSize,
                    TextStyle.body.color,
                    TextAnchor.MiddleCenter,
                    TextAlignment.Center,
                    3);
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
            ConfigureText(titleText, TextStyle.title.characterSize, TextStyle.title.color, TextAnchor.UpperLeft, TextAlignment.Left);
            ConfigureText(bodyText, TextStyle.body.characterSize, TextStyle.body.color, TextAnchor.UpperLeft, TextAlignment.Left);
            ConfigureText(buttonText, TextStyle.body.characterSize, TextStyle.body.color, TextAnchor.MiddleCenter, TextAlignment.Center);
        }

        private void ConfigureText(TextMesh textMesh, float characterSize, Color color, TextAnchor anchor, TextAlignment alignment)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.fontSize = Style.TextFontSize;
            textMesh.characterSize = Mathf.Max(0.001f, characterSize);
            textMesh.color = color;
            textMesh.anchor = anchor;
            textMesh.alignment = alignment;
        }

        private void UpdateMaterialColors()
        {
            SpatialRenderUtility.SetRendererColor(background, Chrome.panelColor);
            SpatialRenderUtility.SetRendererColor(accentBar, Style.AccentColor);

            Color buttonColor = ChoiceStyle.choiceColor;
            if (confirmationReceived)
            {
                buttonColor = ChoiceStyle.selectedColor;
            }
            else if (hoverActive)
            {
                buttonColor = Color.Lerp(ChoiceStyle.gazeColor, ChoiceStyle.selectedColor, Mathf.Clamp01(hoverProgress));
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

            background.transform.localPosition = new Vector3(0f, 0f, LocalZBackground);
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(panelSize.x, panelSize.y, 1f);

            accentBar.SetActive(Chrome.showAccentBar);
            if (Chrome.showAccentBar)
            {
                accentBar.transform.localPosition = new Vector3(-panelSize.x * 0.5f + Chrome.padding.x * 0.5f, 0f, LocalZAccent);
                accentBar.transform.localRotation = Quaternion.identity;
                accentBar.transform.localScale = new Vector3(Style.AccentWidth, Mathf.Max(0.01f, panelSize.y - Chrome.padding.y * 1.5f), 1f);
            }

            float extraLeftInset = Chrome.showAccentBar ? Style.AccentWidth + Chrome.accentGap : 0f;
            float contentLeft = -panelSize.x * 0.5f + Chrome.padding.x + extraLeftInset;
            float contentTop = panelSize.y * 0.5f - Chrome.padding.y;
            float contentWidth = Mathf.Max(0.01f, panelSize.x - Chrome.padding.x * 2f - extraLeftInset);

            titleText.transform.localPosition = new Vector3(contentLeft, contentTop, LocalZText);
            float titleHeight = Mathf.Max(1, SpatialRenderUtility.CountLines(titleText.text)) * TextStyle.title.lineHeight;

            bodyText.transform.localPosition = new Vector3(contentLeft, contentTop - titleHeight - TextStyle.titleBodyGap, LocalZText);

            buttonCard.transform.localPosition = new Vector3(0f, -panelSize.y * 0.5f + buttonBottomInset + buttonSize.y * 0.5f, LocalZButton);
            buttonCard.transform.localRotation = Quaternion.identity;
            buttonCard.transform.localScale = new Vector3(Mathf.Min(buttonSize.x, contentWidth), buttonSize.y, 1f);

            buttonText.transform.localPosition = new Vector3(0f, buttonCard.transform.localPosition.y + TextStyle.body.characterSize * 0.5f, LocalZText + 0.01f);
            buttonText.transform.localRotation = Quaternion.identity;
        }

        private int GetEffectiveWrapCharacters(float characterSize)
        {
            float extraLeftInset = Chrome.showAccentBar ? Style.AccentWidth + Chrome.accentGap : 0f;
            float contentWidth = panelSize.x - Chrome.padding.x * 2f - extraLeftInset;
            float averageCharacterWidth = Mathf.Max(0.001f, characterSize * TextStyle.estimatedCharacterWidth);
            int fit = Mathf.FloorToInt(contentWidth / averageCharacterWidth);
            return Mathf.Max(12, Mathf.Min(Style.WrapCharacters, fit));
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
