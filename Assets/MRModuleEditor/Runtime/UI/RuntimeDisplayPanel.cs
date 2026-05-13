using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class RuntimeDisplayPanel : MonoBehaviour
    {
        private enum PanelMode
        {
            None,
            Text,
            Image,
            MCQ
        }

        private PanelMode mode = PanelMode.None;
        private string title = "";
        private string body = "";
        private Texture2D image;
        private string[] choices = new string[0];
        private int correctIndex = -1;
        private int selectedIndex = -1;
        private string feedback = "";

        public bool HasMcqAnswer
        {
            get { return selectedIndex >= 0; }
        }

        public int SelectedMcqAnswer
        {
            get { return selectedIndex; }
        }

        public void Clear()
        {
            mode = PanelMode.None;
            title = "";
            body = "";
            image = null;
            choices = new string[0];
            correctIndex = -1;
            selectedIndex = -1;
            feedback = "";
        }

        public void ShowText(string newTitle, string newBody)
        {
            mode = PanelMode.Text;
            title = newTitle ?? "";
            body = newBody ?? "";
            image = null;
            choices = new string[0];
            selectedIndex = -1;
            feedback = "";
        }

        public void ShowImage(string newTitle, string newBody, Texture2D newImage)
        {
            mode = PanelMode.Image;
            title = newTitle ?? "";
            body = newBody ?? "";
            image = newImage;
            choices = new string[0];
            selectedIndex = -1;
            feedback = "";
        }

        public void ShowMCQ(string newTitle, string question, string[] newChoices, int newCorrectIndex)
        {
            mode = PanelMode.MCQ;
            title = newTitle ?? "";
            body = question ?? "";
            image = null;
            choices = newChoices ?? new string[0];
            correctIndex = newCorrectIndex;
            selectedIndex = -1;
            feedback = "";
        }

        public void ShowFeedback(string message)
        {
            feedback = message ?? "";
        }

        private void OnGUI()
        {
            if (mode == PanelMode.None)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20, 20, 520, 360), GUI.skin.box);

            if (!string.IsNullOrEmpty(title))
            {
                GUILayout.Label("<b>" + title + "</b>");
            }

            if (!string.IsNullOrEmpty(body))
            {
                GUILayout.Label(body);
            }

            if (mode == PanelMode.Image && image != null)
            {
                GUILayout.Space(8);
                GUILayout.Label(image, GUILayout.Width(360), GUILayout.Height(180));
            }

            if (mode == PanelMode.MCQ)
            {
                GUILayout.Space(8);
                for (int i = 0; i < choices.Length; i++)
                {
                    GUI.enabled = selectedIndex < 0;
                    if (GUILayout.Button(choices[i], GUILayout.Height(32)))
                    {
                        selectedIndex = i;
                        feedback = selectedIndex == correctIndex ? "Correct." : "Not quite. Correct answer: " + SafeChoice(correctIndex);
                    }
                    GUI.enabled = true;
                }
            }

            if (!string.IsNullOrEmpty(feedback))
            {
                GUILayout.Space(8);
                GUILayout.Label(feedback);
            }

            GUILayout.EndArea();
        }

        private string SafeChoice(int index)
        {
            if (choices == null || index < 0 || index >= choices.Length)
            {
                return "unknown";
            }

            return choices[index];
        }
    }
}