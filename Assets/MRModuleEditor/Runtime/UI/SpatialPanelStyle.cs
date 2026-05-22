using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    [CreateAssetMenu(fileName = "SpatialPanelStyle", menuName = "MR Module Editor/UI/Spatial Panel Style")]
    public class SpatialPanelStyle : ScriptableObject
    {
        private static SpatialPanelStyle fallback;

        [Header("Shared")]
        [SerializeField]
        private int wrapCharacters = 50;

        [SerializeField]
        private int textFontSize = 32;

        [SerializeField]
        private float accentWidth = 0.035f;

        [SerializeField]
        private Color accentColor = new Color(0.28f, 0.68f, 1.0f, 0.95f);

        [Header("Panel Defaults")]
        [SerializeField]
        private TextPanelStyle textPanel = new TextPanelStyle();

        [SerializeField]
        private ImagePanelStyle imagePanel = new ImagePanelStyle();

        [SerializeField]
        private MCQPanelStyle mcqPanel = new MCQPanelStyle();

        [Header("Choice Cards")]
        [SerializeField]
        private ChoiceCardStyle choiceCards = new ChoiceCardStyle();

        [Header("Head Follow")]
        [SerializeField]
        private HeadFollowStyle headFollow = new HeadFollowStyle();

        public static SpatialPanelStyle Fallback
        {
            get
            {
                if (fallback == null)
                {
                    fallback = CreateInstance<SpatialPanelStyle>();
                    fallback.name = "Spatial Panel Style Fallback";
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallback;
            }
        }

        public int WrapCharacters
        {
            get { return Mathf.Max(1, wrapCharacters); }
        }

        public int TextFontSize
        {
            get { return Mathf.Max(1, textFontSize); }
        }

        public float AccentWidth
        {
            get { return Mathf.Max(0f, accentWidth); }
        }

        public Color AccentColor
        {
            get { return accentColor; }
        }

        public TextPanelStyle TextPanel
        {
            get
            {
                if (textPanel == null)
                {
                    textPanel = new TextPanelStyle();
                }

                textPanel.Validate();
                return textPanel;
            }
        }

        public ImagePanelStyle ImagePanel
        {
            get
            {
                if (imagePanel == null)
                {
                    imagePanel = new ImagePanelStyle();
                }

                imagePanel.Validate();
                return imagePanel;
            }
        }

        public MCQPanelStyle MCQPanel
        {
            get
            {
                if (mcqPanel == null)
                {
                    mcqPanel = new MCQPanelStyle();
                }

                mcqPanel.Validate();
                return mcqPanel;
            }
        }

        public ChoiceCardStyle ChoiceCards
        {
            get
            {
                if (choiceCards == null)
                {
                    choiceCards = new ChoiceCardStyle();
                }

                choiceCards.Validate();
                return choiceCards;
            }
        }

        public HeadFollowStyle HeadFollow
        {
            get
            {
                if (headFollow == null)
                {
                    headFollow = new HeadFollowStyle();
                }

                headFollow.Validate();
                return headFollow;
            }
        }

        private void OnValidate()
        {
            Validate();
        }

        public void Validate()
        {
            wrapCharacters = Mathf.Max(1, wrapCharacters);
            textFontSize = Mathf.Max(1, textFontSize);
            accentWidth = Mathf.Max(0f, accentWidth);
            TextPanel.Validate();
            ImagePanel.Validate();
            MCQPanel.Validate();
            ChoiceCards.Validate();
            HeadFollow.Validate();
        }

        [System.Serializable]
        public class PanelChromeStyle
        {
            public Color panelColor = new Color(0.03f, 0.03f, 0.03f, 0.88f);
            public Vector2 padding = new Vector2(0.08f, 0.08f);
            public float textDepthOffset = 0.02f;
            public bool showAccentBar = false;
            public float accentGap = 0.05f;

            public PanelChromeStyle()
            {
            }

            public PanelChromeStyle(
                Color panelColor,
                Vector2 padding,
                float textDepthOffset,
                bool showAccentBar,
                float accentGap)
            {
                this.panelColor = panelColor;
                this.padding = padding;
                this.textDepthOffset = textDepthOffset;
                this.showAccentBar = showAccentBar;
                this.accentGap = accentGap;
            }

            public void Validate()
            {
                padding = SpatialRenderUtility.ClampVector(padding, Vector2.zero);
                textDepthOffset = Mathf.Max(0.001f, textDepthOffset);
                accentGap = Mathf.Max(0f, accentGap);
            }
        }

        [System.Serializable]
        public class TextRoleStyle
        {
            public Color color = Color.white;
            public float characterSize = 0.022f;
            public float lineHeight = 0.1f;

            public TextRoleStyle()
            {
            }

            public TextRoleStyle(Color color, float characterSize, float lineHeight)
            {
                this.color = color;
                this.characterSize = characterSize;
                this.lineHeight = lineHeight;
            }

            public void Validate()
            {
                characterSize = Mathf.Max(0.001f, characterSize);
                lineHeight = Mathf.Max(0.001f, lineHeight);
            }
        }

        [System.Serializable]
        public class TextPanelStyle
        {
            public PanelChromeStyle chrome = new PanelChromeStyle(
                new Color(0.03f, 0.03f, 0.03f, 0.86f),
                new Vector2(0.18f, 0.08f),
                0.01f,
                true,
                0.10f);
            public TextRoleStyle title = new TextRoleStyle(Color.white, 0.035f, 0.2f);
            public TextRoleStyle body = new TextRoleStyle(new Color(0.92f, 0.92f, 0.92f, 1f), 0.025f, 0.12f);
            public float titleBodyGap = 0.01f;
            public float estimatedCharacterWidth = 1.5f;

            public void Validate()
            {
                if (chrome == null) chrome = new PanelChromeStyle();
                if (title == null) title = new TextRoleStyle(Color.white, 0.035f, 0.2f);
                if (body == null) body = new TextRoleStyle(new Color(0.92f, 0.92f, 0.92f, 1f), 0.025f, 0.12f);

                chrome.Validate();
                title.Validate();
                body.Validate();
                titleBodyGap = Mathf.Max(0f, titleBodyGap);
                estimatedCharacterWidth = Mathf.Max(0.001f, estimatedCharacterWidth);
            }
        }

        [System.Serializable]
        public class ImagePanelStyle
        {
            public PanelChromeStyle chrome = new PanelChromeStyle(
                new Color(0.03f, 0.03f, 0.03f, 0.88f),
                new Vector2(0.10f, 0.08f),
                0.02f,
                false,
                0.08f);
            public TextRoleStyle title = new TextRoleStyle(Color.white, 0.025f, 0.12f);
            public TextRoleStyle caption = new TextRoleStyle(new Color(0.92f, 0.92f, 0.92f, 1f), 0.018f, 0.075f);
            public float titleImageGap = 0.055f;
            public float imageCaptionGap = 0.055f;
            public float estimatedCharacterWidth = 1.5f;

            public void Validate()
            {
                if (chrome == null) chrome = new PanelChromeStyle();
                if (title == null) title = new TextRoleStyle(Color.white, 0.025f, 0.12f);
                if (caption == null) caption = new TextRoleStyle(new Color(0.92f, 0.92f, 0.92f, 1f), 0.018f, 0.075f);

                chrome.Validate();
                title.Validate();
                caption.Validate();
                titleImageGap = Mathf.Max(0f, titleImageGap);
                imageCaptionGap = Mathf.Max(0f, imageCaptionGap);
                estimatedCharacterWidth = Mathf.Max(0.001f, estimatedCharacterWidth);
            }
        }

        [System.Serializable]
        public class MCQPanelStyle
        {
            public PanelChromeStyle chrome = new PanelChromeStyle(
                new Color(0.03f, 0.03f, 0.03f, 0.90f),
                new Vector2(0.08f, 0.08f),
                0.02f,
                false,
                0.08f);
            public TextRoleStyle title = new TextRoleStyle(Color.white, 0.022f, 0.11f);
            public TextRoleStyle question = new TextRoleStyle(new Color(0.94f, 0.94f, 0.94f, 1f), 0.03f, 0.14f);
            public TextRoleStyle choice = new TextRoleStyle(Color.white, 0.022f, 0.065f);
            public TextRoleStyle feedback = new TextRoleStyle(new Color(0.82f, 0.90f, 1f, 1f), 0.022f, 0.095f);
            public float titleQuestionGap = 0.055f;
            public float questionChoiceGap = 0.09f;
            public float feedbackGapBelowChoices = 0.045f;
            public float estimatedCharacterWidth = 1.8f;

            public void Validate()
            {
                if (chrome == null) chrome = new PanelChromeStyle();
                if (title == null) title = new TextRoleStyle(Color.white, 0.022f, 0.11f);
                if (question == null) question = new TextRoleStyle(new Color(0.94f, 0.94f, 0.94f, 1f), 0.03f, 0.14f);
                if (choice == null) choice = new TextRoleStyle(Color.white, 0.022f, 0.065f);
                if (feedback == null) feedback = new TextRoleStyle(new Color(0.82f, 0.90f, 1f, 1f), 0.022f, 0.095f);

                chrome.Validate();
                title.Validate();
                question.Validate();
                choice.Validate();
                feedback.Validate();
                titleQuestionGap = Mathf.Max(0f, titleQuestionGap);
                questionChoiceGap = Mathf.Max(0f, questionChoiceGap);
                feedbackGapBelowChoices = Mathf.Max(0f, feedbackGapBelowChoices);
                estimatedCharacterWidth = Mathf.Max(0.001f, estimatedCharacterWidth);
            }
        }

        [System.Serializable]
        public class ChoiceCardStyle
        {
            public Color choiceColor = new Color(0.16f, 0.20f, 0.26f, 0.96f);
            public Color gazeColor = new Color(0.25f, 0.42f, 0.72f, 0.96f);
            public Color selectedColor = new Color(0.20f, 0.55f, 0.35f, 0.96f);
            public Color wrongColor = new Color(0.62f, 0.20f, 0.20f, 0.96f);

            public void Validate()
            {
            }
        }

        [System.Serializable]
        public class HeadFollowStyle
        {
            public bool smoothFollow = true;
            public float followSharpness = 16f;
            public float snapDistance = 2.5f;

            public void Validate()
            {
                followSharpness = Mathf.Max(0.01f, followSharpness);
                snapDistance = Mathf.Max(0.01f, snapDistance);
            }
        }
    }
}