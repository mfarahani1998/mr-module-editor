using System.IO;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class TemporaryHelperMenu
    {
        private const string SampleModuleFolder =
            "Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini";

        private const string StreamingAssetsTargetFolder =
            "Assets/StreamingAssets/MRModuleEditor/SampleModules/ForwardKinematicsMini";

        private const string IntroImageRelativePath = "assets/images/intro.png";

        [MenuItem("MR Module Editor/Templates/Create Placeholder Intro Image")]
        public static void CreatePlaceholderIntroImage()
        {
            string imagePath = Path.Combine(SampleModuleFolder, IntroImageRelativePath)
                .Replace("\\", "/");

            string directory = Path.GetDirectoryName(imagePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Texture2D texture = new Texture2D(1024, 512, TextureFormat.RGBA32, false);

            Color top = new Color(0.06f, 0.10f, 0.16f, 1f);
            Color bottom = new Color(0.12f, 0.18f, 0.28f, 1f);

            for (int y = 0; y < texture.height; y++)
            {
                float t = y / (float)(texture.height - 1);
                Color rowColor = Color.Lerp(bottom, top, t);
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, rowColor);
                }
            }

            // Simple robot-ish blocks. This is only a placeholder image, not artwork.
            FillRect(texture, 120, 120, 170, 42, new Color(0.25f, 0.65f, 1f, 1f));
            FillRect(texture, 280, 245, 220, 36, new Color(0.45f, 0.85f, 1f, 1f));
            FillRect(texture, 488, 340, 190, 32, new Color(0.95f, 0.72f, 0.25f, 1f));
            FillRect(texture, 96, 82, 760, 8, new Color(1f, 1f, 1f, 0.45f));
            FillRect(texture, 96, 420, 760, 8, new Color(1f, 1f, 1f, 0.45f));

            texture.Apply();

            byte[] pngBytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            File.WriteAllBytes(imagePath, pngBytes);
            AssetDatabase.ImportAsset(imagePath);
            AssetDatabase.Refresh();

            Debug.Log("Created placeholder intro image at: " + imagePath);
        }

        [MenuItem("MR Module Editor/Authoring/Copy FK Sample To StreamingAssets")]
        public static void CopyForwardKinematicsSampleToStreamingAssets()
        {
            string sampleImagePath = Path.Combine(SampleModuleFolder, IntroImageRelativePath)
                .Replace("\\", "/");

            if (!File.Exists(sampleImagePath))
            {
                CreatePlaceholderIntroImage();
            }

            if (Directory.Exists(StreamingAssetsTargetFolder))
            {
                Directory.Delete(StreamingAssetsTargetFolder, true);
            }

            CopyDirectoryWithoutMetaFiles(SampleModuleFolder, StreamingAssetsTargetFolder);
            AssetDatabase.Refresh();

            Debug.Log("Copied ForwardKinematicsMini sample to StreamingAssets: "
                + StreamingAssetsTargetFolder);
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            int maxX = Mathf.Min(texture.width, x + width);
            int maxY = Mathf.Min(texture.height, y + height);

            for (int yy = Mathf.Max(0, y); yy < maxY; yy++)
            {
                for (int xx = Mathf.Max(0, x); xx < maxX; xx++)
                {
                    texture.SetPixel(xx, yy, color);
                }
            }
        }

        private static void CopyDirectoryWithoutMetaFiles(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
            {
                if (sourceFile.EndsWith(".meta"))
                {
                    continue;
                }

                string fileName = Path.GetFileName(sourceFile);
                string targetFile = Path.Combine(targetDirectory, fileName);
                File.Copy(sourceFile, targetFile, true);
            }

            foreach (string sourceSubdirectory in Directory.GetDirectories(sourceDirectory))
            {
                string directoryName = Path.GetFileName(sourceSubdirectory);
                string targetSubdirectory = Path.Combine(targetDirectory, directoryName);
                CopyDirectoryWithoutMetaFiles(sourceSubdirectory, targetSubdirectory);
            }
        }
    }
}