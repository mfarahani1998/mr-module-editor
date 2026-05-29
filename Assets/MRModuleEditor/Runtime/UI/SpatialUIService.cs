using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialUIService : MonoBehaviour
    {
        [SerializeField]
        private SpatialTextPanel textPanel;

        [SerializeField]
        private SpatialImagePanel imagePanel;

        [SerializeField]
        private SpatialMCQPanel mcqPanel;

        [SerializeField]
        private SpatialConfirmPanel confirmPanel;

        public bool CanShowText
        {
            get { EnsurePanels(); return textPanel != null; }
        }

        public bool CanShowImage
        {
            get { EnsurePanels(); return imagePanel != null; }
        }

        public bool CanShowMCQ
        {
            get { EnsurePanels(); return mcqPanel != null; }
        }

        public bool CanShowConfirm
        {
            get { EnsurePanels(); return confirmPanel != null; }
        }

        public bool HasMCQAnswer
        {
            get { EnsurePanels(); return mcqPanel != null && mcqPanel.HasAnswer; }
        }

        public int SelectedMCQAnswer
        {
            get { EnsurePanels(); return mcqPanel == null ? -1 : mcqPanel.SelectedAnswer; }
        }

        public bool HasConfirmation
        {
            get { EnsurePanels(); return confirmPanel != null && confirmPanel.HasConfirmation; }
        }

        private void Awake()
        {
            EnsurePanels();
        }

        public void ShowText(ModuleDocument module, ModuleStep step, string text)
        {
            EnsurePanels();

            if (textPanel != null)
            {
                textPanel.ShowText(module, step, text);
            }
        }

        public void ShowText(
            ModuleDocument module,
            ModuleStep step,
            string text,
            Vector3 fallbackLocalOffset,
            Vector3 fallbackLocalEuler,
            Vector3 fallbackLocalScale)
        {
            EnsurePanels();

            if (textPanel != null)
            {
                textPanel.ShowText(module, step, text, fallbackLocalOffset, fallbackLocalEuler, fallbackLocalScale);
            }
        }

        public void ShowImage(ModuleDocument module, ModuleStep step, Texture2D texture, string caption)
        {
            EnsurePanels();

            if (imagePanel != null)
            {
                imagePanel.ShowImage(module, step, texture, caption);
                return;
            }

            if (textPanel != null)
            {
                string fallbackText = string.IsNullOrWhiteSpace(caption)
                    ? (step == null ? "" : step.title ?? "")
                    : caption;
                textPanel.ShowText(module, step, fallbackText);
            }
        }

        public void ShowMCQ(ModuleDocument module, ModuleStep step, string question, string[] choices, int correctIndex)
        {
            EnsurePanels();

            // Keep the MCQ visually focused. This preserves the existing MCQStepHandler behavior
            // where the text panel is cleared before showing a question.
            if (textPanel != null)
            {
                textPanel.Clear();
            }

            if (imagePanel != null)
            {
                imagePanel.Clear();
            }

            if (confirmPanel != null)
            {
                confirmPanel.Clear();
            }

            if (mcqPanel != null)
            {
                mcqPanel.ShowMCQ(module, step, question, choices, correctIndex);
            }
        }

        public void ShowConfirm(ModuleDocument module, ModuleStep step, string message, string buttonLabel)
        {
            EnsurePanels();

            if (textPanel != null)
            {
                textPanel.Clear();
            }

            if (imagePanel != null)
            {
                imagePanel.Clear();
            }

            if (mcqPanel != null)
            {
                mcqPanel.Clear();
            }

            if (confirmPanel != null)
            {
                confirmPanel.ShowConfirm(module, step, message, buttonLabel);
            }
            else if (textPanel != null)
            {
                textPanel.ShowText(module, step, message);
            }
        }

        public void ShowMCQFeedback(string message)
        {
            EnsurePanels();

            if (mcqPanel != null)
            {
                mcqPanel.ShowFeedback(message);
            }
        }

        public void ClearStep(string stepId)
        {
            EnsurePanels();

            if (textPanel != null) textPanel.ClearIfShowingStep(stepId);
            if (imagePanel != null) imagePanel.ClearIfShowingStep(stepId);
            if (mcqPanel != null) mcqPanel.ClearIfShowingStep(stepId);
            if (confirmPanel != null) confirmPanel.ClearIfShowingStep(stepId);
        }

        public void ClearAll()
        {
            EnsurePanels();

            if (textPanel != null) textPanel.Clear();
            if (imagePanel != null) imagePanel.Clear();
            if (mcqPanel != null) mcqPanel.Clear();
            if (confirmPanel != null) confirmPanel.Clear();
        }

        private void EnsurePanels()
        {
            if (textPanel == null)
            {
                textPanel = FindFirstObjectByType<SpatialTextPanel>(FindObjectsInactive.Include);
            }

            if (imagePanel == null)
            {
                imagePanel = FindFirstObjectByType<SpatialImagePanel>(FindObjectsInactive.Include);
            }

            if (mcqPanel == null)
            {
                mcqPanel = FindFirstObjectByType<SpatialMCQPanel>(FindObjectsInactive.Include);
            }

            if (confirmPanel == null)
            {
                confirmPanel = FindFirstObjectByType<SpatialConfirmPanel>(FindObjectsInactive.Include);
            }
        }
    }
}