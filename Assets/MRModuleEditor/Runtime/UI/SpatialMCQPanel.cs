using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using UnityEngine;
using UnityEngine.Rendering;
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
        }

        private const float LocalZBackground = 0f;
        private const float LocalZChoiceCard = 0.01f;
        private const float LocalZTitleQuestion = 0.02f;
        private const float LocalZChoiceText = 0.03f;
        private const float LocalZFeedbackText = 0.03f;
        private const float ColliderDepth = 0.04f;

        private const int SortingBackground = 0;
        private const int SortingChoiceCard = 1;
        private const int SortingText = 2;

        private const float PaddingX = 0.08f;
        private const float PaddingY = 0.08f;
        private const float TitleLineHeight = 0.13f;
        private const float QuestionLineHeight = 0.12f;
        private const float TitleQuestionGap = 0.055f;
        private const float QuestionChoiceGap = 0.09f;
        private const float ChoiceTextInsetX = 0.055f;
        private const float ChoiceTextTopOffset = 0.045f;
        private const float FeedbackGapBelowChoices = 0.045f;

        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private Vector2 panelSize = new Vector2(1.75f, 1.45f);

        [SerializeField]
        private Vector3 defaultLocalOffset = new Vector3(0f, -0.15f, 0f);

        [SerializeField]
        private float choiceHeight = 0.18f;

        [SerializeField]
        private float choiceGap = 0.04f;

        [SerializeField]
        private int wrapCharacters = 44;

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

        [SerializeField]
        private Color panelColor = new Color(0.03f, 0.03f, 0.03f, 0.90f);

        [SerializeField]
        private Color choiceColor = new Color(0.16f, 0.20f, 0.26f, 0.96f);

        [SerializeField]
        private Color gazeColor = new Color(0.25f, 0.42f, 0.72f, 0.96f);

        [SerializeField]
        private Color selectedColor = new Color(0.20f, 0.55f, 0.35f, 0.96f);

        [SerializeField]
        private Color wrongColor = new Color(0.62f, 0.20f, 0.20f, 0.96f);

        private GameObject background;
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
            gameObject.SetActive(false);
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

            titleText.text = step == null ? "Quick Check" : step.title ?? "Quick Check";
            questionText.text = Wrap(question ?? "", wrapCharacters);
            feedbackText.text = BuildInputInstruction();

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
                feedbackText.text = message ?? "";
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
            if (!string.IsNullOrEmpty(stepId) && showingStepId == stepId)
            {
                Clear();
            }
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

            if (titleText != null) titleText.text = "";
            if (questionText != null) questionText.text = "";
            if (feedbackText != null) feedbackText.text = "";

            ClearChoiceVisuals();
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

            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            if (anchorResolver == null)
            {
                return false;
            }

            LayoutDefinition layout = FindLayoutForTarget(currentModule, currentStep.id);
            string anchorId = ResolveAnchorId(layout);

            Pose anchorPose;
            string error;
            if (!anchorResolver.TryResolveAnchor(currentModule, anchorId, out anchorPose, out error))
            {
                return false;
            }

            Vector3 localPosition = layout == null
                ? defaultLocalOffset
                : RuntimeLayoutApplier.ToVector3(layout.position, defaultLocalOffset);

            Vector3 localEuler = layout == null
                ? Vector3.zero
                : RuntimeLayoutApplier.ToVector3(layout.rotationEuler, Vector3.zero);

            Vector3 localScale = layout == null
                ? Vector3.one
                : RuntimeLayoutApplier.ToVector3(layout.scale, Vector3.one);

            transform.position = anchorPose.position + anchorPose.rotation * localPosition;
            transform.rotation = anchorPose.rotation * Quaternion.Euler(localEuler);
            transform.localScale = localScale;
            return true;
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
            if (gazeEnabledForCurrentQuestion && enableKeyboardNumbers)
            {
                return "Look away once, then look at an answer for "
                    + gazeDwellSeconds.ToString("0.0")
                    + "s; or press 1-4.";
            }

            if (gazeEnabledForCurrentQuestion)
            {
                return "Look away once, then look at an answer for "
                    + gazeDwellSeconds.ToString("0.0")
                    + "s.";
            }

            if (enableKeyboardNumbers)
            {
                return "Press 1-4 to answer.";
            }

            return "Choose an answer.";
        }

        private bool IsCurrentStepHeadAnchored()
        {
            if (currentModule == null || currentStep == null)
            {
                return false;
            }

            LayoutDefinition layout = FindLayoutForTarget(currentModule, currentStep.id);
            string anchorId = ResolveAnchorId(layout);
            AnchorDefinition anchor = FindAnchor(currentModule, anchorId);
            return anchor != null && anchor.type == "head";
        }

        private string ResolveAnchorId(LayoutDefinition layout)
        {
            string anchorId = layout == null ? "" : layout.anchorId;
            if (string.IsNullOrWhiteSpace(anchorId) && currentStep != null)
            {
                anchorId = currentStep.GetString("anchorId", "anchor.head.default");
            }

            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = "anchor.head.default";
            }

            return anchorId;
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
                background = CreateQuad("MCQ Background", MakeMaterial(panelColor), false, SortingBackground);
                background.transform.SetParent(transform, false);
                background.transform.localScale = new Vector3(panelSize.x, panelSize.y, 1f);
            }

            if (titleText == null)
            {
                titleText = CreateText("Title", 0.035f, Color.white, SortingText);
                titleText.transform.localPosition = new Vector3(-panelSize.x * 0.5f + 0.08f, panelSize.y * 0.5f - 0.07f, 0.02f);
            }

            if (questionText == null)
            {
                questionText = CreateText("Question", 0.025f, new Color(0.94f, 0.94f, 0.94f, 1f), SortingText);
                questionText.transform.localPosition = new Vector3(-panelSize.x * 0.5f + 0.08f, panelSize.y * 0.5f - 0.18f, 0.02f);
            }

            if (feedbackText == null)
            {
                feedbackText = CreateText("Feedback", 0.022f, new Color(0.82f, 0.90f, 1f, 1f), SortingText);
                feedbackText.transform.localPosition = new Vector3(-panelSize.x * 0.5f + 0.08f, -panelSize.y * 0.5f + 0.11f, 0.02f);
            }

            UpdateVisualLayout();
        }

        private void RebuildChoiceVisuals()
        {
            ClearChoiceVisuals();

            for (int i = 0; i < choices.Length; i++)
            {
                GameObject card = CreateQuad(
                    "Choice " + (i + 1),
                    MakeMaterial(choiceColor),
                    true,
                    SortingChoiceCard);
                card.transform.SetParent(transform, false);

                TextMesh text = CreateText("Choice Text " + (i + 1), 0.022f, Color.white, SortingText);
                text.text = (i + 1) + ". " + Wrap(choices[i], wrapCharacters);

                ChoiceVisual visual = new ChoiceVisual();
                visual.card = card;
                visual.text = text;
                visual.renderer = card.GetComponent<Renderer>();
                choiceVisuals.Add(visual);
            }

            UpdateVisualLayout();
            UpdateChoiceColors();
        }

        private void UpdateVisualLayout()
        {
            if (background == null || titleText == null || questionText == null || feedbackText == null)
            {
                return;
            }

            background.transform.localPosition = new Vector3(0f, 0f, LocalZBackground);
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(panelSize.x, panelSize.y, 1f);

            float left = -panelSize.x * 0.5f + PaddingX;
            float top = panelSize.y * 0.5f - PaddingY;
            float choiceWidth = panelSize.x - PaddingX * 2f;

            titleText.transform.localPosition = new Vector3(left, top, LocalZTitleQuestion);

            int titleLines = Mathf.Max(1, CountLines(titleText.text));
            float questionY = top - titleLines * TitleLineHeight - TitleQuestionGap;
            questionText.transform.localPosition = new Vector3(left, questionY, LocalZTitleQuestion);

            int questionLines = Mathf.Max(1, CountLines(questionText.text));
            float firstChoiceCenterY = questionY
                - questionLines * QuestionLineHeight
                - QuestionChoiceGap
                - choiceHeight * 0.5f;

            float lastChoiceBottomY = firstChoiceCenterY;

            for (int i = 0; i < choiceVisuals.Count; i++)
            {
                ChoiceVisual visual = choiceVisuals[i];
                float centerY = firstChoiceCenterY - i * (choiceHeight + choiceGap);
                lastChoiceBottomY = centerY - choiceHeight * 0.5f;

                if (visual.card != null)
                {
                    visual.card.transform.localPosition = new Vector3(0f, centerY, LocalZChoiceCard);
                    visual.card.transform.localRotation = Quaternion.identity;
                    visual.card.transform.localScale = new Vector3(choiceWidth, choiceHeight, 1f);
                }

                if (visual.text != null)
                {
                    visual.text.transform.localPosition = new Vector3(
                        -choiceWidth * 0.5f + ChoiceTextInsetX,
                        centerY + choiceHeight * 0.5f - ChoiceTextTopOffset,
                        LocalZChoiceText);
                }
            }

            float feedbackY = lastChoiceBottomY - FeedbackGapBelowChoices;
            float bottomSafeY = -panelSize.y * 0.5f + PaddingY + 0.08f;
            feedbackY = Mathf.Max(bottomSafeY, feedbackY);
            feedbackText.transform.localPosition = new Vector3(left, feedbackY, LocalZFeedbackText);
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                }
            }

            return count;
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
                    SetMaterialColor(choiceVisuals[i].renderer.sharedMaterial, color);
                }
            }
        }

        private GameObject CreateQuad(
            string objectName,
            Material material,
            bool keepCollider,
            int sortingOrder)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = objectName;

            Collider collider = quad.GetComponent<Collider>();
            if (!keepCollider)
            {
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
            else
            {
                if (collider != null)
                {
                    Destroy(collider);
                }

                BoxCollider boxCollider = quad.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1f, 1f, ColliderDepth);
            }

            Renderer renderer = quad.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = sortingOrder;
            }

            return quad;
        }

        private TextMesh CreateText(
            string objectName,
            float characterSize,
            Color color,
            int sortingOrder)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(transform, false);

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.UpperLeft;
            textMesh.alignment = TextAlignment.Left;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
            textMesh.text = "";

            Renderer renderer = textObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = sortingOrder;
            }

            return textMesh;
        }

        private static Material MakeMaterial(Color color)
        {
            return SpatialMaterialUtility.CreateColorMaterial(color, nameof(SpatialMCQPanel));
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            SpatialMaterialUtility.SetMaterialColor(material, color);
        }

        private static LayoutDefinition FindLayoutForTarget(ModuleDocument module, string targetId)
        {
            if (module == null || module.layouts == null || string.IsNullOrWhiteSpace(targetId))
            {
                return null;
            }

            for (int i = 0; i < module.layouts.Count; i++)
            {
                LayoutDefinition layout = module.layouts[i];
                if (layout != null && layout.targetId == targetId)
                {
                    return layout;
                }
            }

            return null;
        }

        private static AnchorDefinition FindAnchor(ModuleDocument module, string anchorId)
        {
            if (module == null || module.anchors == null || string.IsNullOrWhiteSpace(anchorId))
            {
                return null;
            }

            for (int i = 0; i < module.anchors.Count; i++)
            {
                AnchorDefinition anchor = module.anchors[i];
                if (anchor != null && anchor.id == anchorId)
                {
                    return anchor;
                }
            }

            return null;
        }

        private static string Wrap(string text, int maxCharactersPerLine)
        {
            if (string.IsNullOrEmpty(text) || maxCharactersPerLine <= 0)
            {
                return text ?? "";
            }

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] words = text.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int lineLength = 0;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (lineLength > 0 && lineLength + word.Length + 1 > maxCharactersPerLine)
                {
                    builder.Append('\n');
                    lineLength = 0;
                }

                if (lineLength > 0)
                {
                    builder.Append(' ');
                    lineLength++;
                }

                builder.Append(word);
                lineLength += word.Length;
            }

            return builder.ToString();
        }

        private void OnValidate()
        {
            panelSize = new Vector2(Mathf.Max(0.1f, panelSize.x), Mathf.Max(0.1f, panelSize.y));
            choiceHeight = Mathf.Max(0.02f, choiceHeight);
            choiceGap = Mathf.Max(0f, choiceGap);
            wrapCharacters = Mathf.Max(8, wrapCharacters);
            gazeDwellSeconds = Mathf.Max(0.05f, gazeDwellSeconds);
            gazeInputArmDelaySeconds = Mathf.Max(0f, gazeInputArmDelaySeconds);
            gazeRayDistance = Mathf.Max(0.1f, gazeRayDistance);

            if (background != null)
            {
                UpdateVisualLayout();
            }
        }
    }
}