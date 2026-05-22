using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialTextPanel : MonoBehaviour
    {
        [SerializeField]
        private SpatialLayoutResolver spatialLayoutResolver;

        [Header("Style")]
        [SerializeField]
        private SpatialPanelStyle style;

        [Header("Panel")]
        [SerializeField]
        private bool autoSizePanel = true;

        // Used as the fixed size when autoSizePanel is disabled.
        [SerializeField]
        private Vector2 panelSize = new Vector2(3.2f, 0.58f);

        [SerializeField]
        private Vector3 panelLocalOffset = new Vector3(0f, -1f, 0f);

        [SerializeField]
        private bool applyPanelLocalOffsetToAuthoredLayouts = false;

        [SerializeField]
        private Vector2 minimumPanelSize = new Vector2(0.8f, 0.25f);

        [SerializeField]
        private Vector2 maximumPanelSize = new Vector2(1.5f, 0.6f);

        private GameObject background;
        private GameObject accentBar;
        private TextMesh titleText;
        private TextMesh bodyText;

        private ModuleDocument currentModule;
        private ModuleStep currentStep;
        private string showingStepId = "";
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

        private SpatialPanelStyle.TextPanelStyle TextStyle
        {
            get { return Style.TextPanel; }
        }

        private SpatialPanelStyle.PanelChromeStyle Chrome
        {
            get { return TextStyle.chrome; }
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
            if (background != null && titleText != null && bodyText != null)
            {
                ApplyTextSettings();
                UpdateMaterialColors();
                UpdateVisualLayout();
            }
        }

        public void ShowText(ModuleDocument module, ModuleStep step, string body)
        {
            EnsureVisuals();

            currentModule = module;
            currentStep = step;
            showingStepId = step == null ? "" : step.id;
            hasAppliedPose = false;

            titleText.text = step == null ? "" : step.title ?? "";
            bodyText.text = SpatialRenderUtility.Wrap(body ?? "", GetEffectiveWrapCharacters());

            ApplyTextSettings();
            UpdateVisualLayout();

            gameObject.SetActive(true);
            ApplyAnchoredPose();
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
            hasAppliedPose = false;

            if (titleText != null) titleText.text = "";
            if (bodyText != null) bodyText.text = "";
            if (background != null) UpdateVisualLayout();

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            ApplyAnchoredPose();
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
                    "Spatial Panel Background",
                    SpatialRenderUtility.CreateTransparentColorMaterial(Chrome.panelColor, nameof(SpatialTextPanel)),
                    false,
                    0);
            }

            if (accentBar == null)
            {
                accentBar = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Spatial Panel Accent",
                    SpatialRenderUtility.CreateTransparentColorMaterial(Style.AccentColor, nameof(SpatialTextPanel)),
                    false,
                    1);
            }

            if (titleText == null)
            {
                titleText = SpatialRenderUtility.CreateText(
                    transform,
                    "Title",
                    TextStyle.title.characterSize,
                    Style.TextFontSize,
                    TextStyle.title.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    2);
            }

            if (bodyText == null)
            {
                bodyText = SpatialRenderUtility.CreateText(
                    transform,
                    "Body",
                    TextStyle.body.characterSize,
                    Style.TextFontSize,
                    TextStyle.body.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    2);
            }

            ApplyTextSettings();
            UpdateMaterialColors();
            UpdateVisualLayout();
        }

        private void ApplyTextSettings()
        {
            if (titleText != null)
            {
                titleText.anchor = TextAnchor.UpperLeft;
                titleText.alignment = TextAlignment.Left;
                titleText.fontSize = Style.TextFontSize;
                titleText.characterSize = TextStyle.title.characterSize;
                titleText.color = TextStyle.title.color;
            }

            if (bodyText != null)
            {
                bodyText.anchor = TextAnchor.UpperLeft;
                bodyText.alignment = TextAlignment.Left;
                bodyText.fontSize = Style.TextFontSize;
                bodyText.characterSize = TextStyle.body.characterSize;
                bodyText.color = TextStyle.body.color;
            }
        }

        private void UpdateMaterialColors()
        {
            SpatialRenderUtility.SetRendererColor(background, Chrome.panelColor);
            SpatialRenderUtility.SetRendererColor(accentBar, Style.AccentColor);
        }

        private void UpdateVisualLayout()
        {
            if (background == null || titleText == null || bodyText == null)
            {
                return;
            }

            string title = titleText.text ?? "";
            string body = bodyText.text ?? "";
            bool hasTitle = !string.IsNullOrWhiteSpace(title);
            bool hasBody = !string.IsNullOrWhiteSpace(body);

            Vector2 actualSize = autoSizePanel ? CalculatePanelSize(title, body, hasTitle, hasBody) : panelSize;
            actualSize = new Vector2(Mathf.Max(0.1f, actualSize.x), Mathf.Max(0.1f, actualSize.y));

            background.transform.localPosition = Vector3.zero;
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(actualSize.x, actualSize.y, 1f);

            float extraLeftInset = Chrome.showAccentBar ? Style.AccentWidth + Chrome.accentGap : 0f;
            float left = -actualSize.x * 0.5f + Chrome.padding.x + extraLeftInset;
            float top = actualSize.y * 0.5f - Chrome.padding.y;

            if (accentBar != null)
            {
                accentBar.SetActive(Chrome.showAccentBar);
                accentBar.transform.localPosition = new Vector3(
                    -actualSize.x * 0.5f + Chrome.padding.x * 0.5f,
                    0f,
                    Chrome.textDepthOffset * 0.5f);
                accentBar.transform.localRotation = Quaternion.identity;
                accentBar.transform.localScale = new Vector3(
                    Style.AccentWidth,
                    Mathf.Max(0.01f, actualSize.y - Chrome.padding.y * 1.5f),
                    1f);
            }

            titleText.transform.localPosition = new Vector3(left, top, Chrome.textDepthOffset);

            float bodyTop = top;
            if (hasTitle)
            {
                bodyTop -= SpatialRenderUtility.CountLines(title) * TextStyle.title.lineHeight;
            }

            if (hasTitle && hasBody)
            {
                bodyTop -= TextStyle.titleBodyGap;
            }

            bodyText.transform.localPosition = new Vector3(left, bodyTop, Chrome.textDepthOffset);
        }

        private Vector2 CalculatePanelSize(string title, string body, bool hasTitle, bool hasBody)
        {
            float extraLeftInset = Chrome.showAccentBar ? Style.AccentWidth + Chrome.accentGap : 0f;

            float titleWidth = SpatialRenderUtility.LongestLineLength(title) * TextStyle.title.characterSize * TextStyle.estimatedCharacterWidth;
            float bodyWidth = SpatialRenderUtility.LongestLineLength(body) * TextStyle.body.characterSize * TextStyle.estimatedCharacterWidth;
            float desiredWidth = Mathf.Max(titleWidth, bodyWidth) + Chrome.padding.x * 2f + extraLeftInset;
            desiredWidth = Mathf.Clamp(desiredWidth, minimumPanelSize.x, maximumPanelSize.x);

            int titleLineCount = hasTitle ? SpatialRenderUtility.CountLines(title) : 0;
            int bodyLineCount = hasBody ? SpatialRenderUtility.CountLines(body) : 0;

            float desiredHeight = Chrome.padding.y * 2f;
            if (hasTitle)
            {
                desiredHeight += titleLineCount * TextStyle.title.lineHeight;
            }

            if (hasTitle && hasBody)
            {
                desiredHeight += TextStyle.titleBodyGap;
            }

            if (hasBody)
            {
                desiredHeight += bodyLineCount * TextStyle.body.lineHeight;
            }

            if (!hasTitle && !hasBody)
            {
                desiredHeight += TextStyle.body.lineHeight;
            }

            desiredHeight = Mathf.Clamp(desiredHeight, minimumPanelSize.y, maximumPanelSize.y);
            return new Vector2(desiredWidth, desiredHeight);
        }

        private int GetEffectiveWrapCharacters()
        {
            int result = Mathf.Max(1, Style.WrapCharacters);
            if (!autoSizePanel)
            {
                return result;
            }

            float extraLeftInset = Chrome.showAccentBar ? Style.AccentWidth + Chrome.accentGap : 0f;
            float contentWidth = maximumPanelSize.x - Chrome.padding.x * 2f - extraLeftInset;
            float averageCharacterWidth = Mathf.Max(0.001f, TextStyle.body.characterSize * TextStyle.estimatedCharacterWidth);
            int fitAtMaxWidth = Mathf.FloorToInt(contentWidth / averageCharacterWidth);

            if (fitAtMaxWidth > 0)
            {
                result = Mathf.Min(result, fitAtMaxWidth);
            }

            return Mathf.Max(12, result);
        }

    }
}