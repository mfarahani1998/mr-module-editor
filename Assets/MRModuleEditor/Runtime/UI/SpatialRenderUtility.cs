using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace MRModuleEditor.Runtime.UI
{
    public static class SpatialRenderUtility
    {
        public static GameObject CreateQuad(
            Transform parent,
            string objectName,
            Material material,
            bool keepCollider,
            int sortingOrder,
            float colliderDepth = 0.04f)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = objectName;

            if (parent != null)
            {
                quad.transform.SetParent(parent, false);
            }

            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;

            Collider existingCollider = quad.GetComponent<Collider>();
            if (existingCollider != null)
            {
                DestroyUnityObject(existingCollider);
            }

            if (keepCollider)
            {
                BoxCollider boxCollider = quad.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1f, 1f, Mathf.Max(0.001f, colliderDepth));
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

        public static TextMesh CreateText(
            Transform parent,
            string objectName,
            float characterSize,
            int fontSize,
            Color color,
            TextAnchor anchor,
            TextAlignment alignment,
            int sortingOrder)
        {
            GameObject textObject = new GameObject(objectName);

            if (parent != null)
            {
                textObject.transform.SetParent(parent, false);
            }

            textObject.transform.localPosition = Vector3.zero;
            textObject.transform.localRotation = Quaternion.identity;

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = anchor;
            textMesh.alignment = alignment;
            textMesh.fontSize = Mathf.Max(1, fontSize);
            textMesh.characterSize = Mathf.Max(0.001f, characterSize);
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

        public static Material CreateTransparentColorMaterial(Color color, string owner)
        {
            return SpatialMaterialUtility.CreateColorMaterial(color, owner);
        }

        public static Material CreateImageMaterial(string owner)
        {
            return SpatialMaterialUtility.CreateTextureMaterial(owner);
        }

        public static void SetRendererColor(GameObject target, Color color)
        {
            if (target == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            SetMaterialColor(renderer.sharedMaterial, color);
        }

        public static void SetMaterialColor(Material material, Color color)
        {
            SpatialMaterialUtility.SetMaterialColor(material, color);
        }

        public static void SetMaterialTexture(Material material, Texture texture)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }

        public static string Wrap(string text, int maxCharactersPerLine)
        {
            if (string.IsNullOrEmpty(text) || maxCharactersPerLine <= 0)
            {
                return text ?? "";
            }

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] paragraphs = text.Split('\n');
            StringBuilder builder = new StringBuilder();

            for (int p = 0; p < paragraphs.Length; p++)
            {
                if (p > 0)
                {
                    builder.Append('\n');
                }

                string paragraph = paragraphs[p];
                if (string.IsNullOrWhiteSpace(paragraph))
                {
                    continue;
                }

                string[] words = paragraph.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                int currentLineLength = 0;

                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];

                    while (word.Length > maxCharactersPerLine)
                    {
                        if (currentLineLength > 0)
                        {
                            builder.Append('\n');
                            currentLineLength = 0;
                        }

                        builder.Append(word.Substring(0, maxCharactersPerLine));
                        word = word.Substring(maxCharactersPerLine);

                        if (word.Length > 0)
                        {
                            builder.Append('\n');
                        }
                    }

                    if (word.Length == 0)
                    {
                        continue;
                    }

                    if (currentLineLength > 0 && currentLineLength + word.Length + 1 > maxCharactersPerLine)
                    {
                        builder.Append('\n');
                        currentLineLength = 0;
                    }
                    else if (currentLineLength > 0)
                    {
                        builder.Append(' ');
                        currentLineLength++;
                    }

                    builder.Append(word);
                    currentLineLength += word.Length;
                }
            }

            return builder.ToString();
        }

        public static int CountLines(string text)
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

        public static int LongestLineLength(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int longest = 0;
            int current = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    longest = Mathf.Max(longest, current);
                    current = 0;
                }
                else if (c != '\r')
                {
                    current++;
                }
            }

            return Mathf.Max(longest, current);
        }

        public static Vector2 ClampVector(Vector2 value, Vector2 minimum)
        {
            return new Vector2(Mathf.Max(minimum.x, value.x), Mathf.Max(minimum.y, value.y));
        }

        private static void DestroyUnityObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}