using System;
using MRModuleEditor.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MRModuleEditor.Authoring.Editor
{
    public static class RuntimePreviewScenePathUtility
    {
        public const string RuntimePreviewScenePath = "Assets/MRModuleEditor/Samples/Scenes/RuntimePreview.unity";
        public const string RuntimePreviewHeadsetScenePath = "Assets/MRModuleEditor/Samples/Scenes/RuntimePreview_Headset.unity";

        private static readonly string[] PreviewScenePaths =
        {
            RuntimePreviewScenePath,
            RuntimePreviewHeadsetScenePath
        };

        public static bool PersistAssetsRelativeModulePathForPreviewScenes(
            string relativePathFromAssets,
            out string error)
        {
            string normalizedPath = NormalizeSerializedPath(relativePathFromAssets);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                error = "Assets-relative module path is empty.";
                return false;
            }

            return PersistLoaderPathsForScenes(
                PreviewScenePaths,
                (scenePath, loader) =>
                {
                    if (string.Equals(scenePath, RuntimePreviewScenePath, StringComparison.OrdinalIgnoreCase))
                    {
                        loader.LoadMode = ModuleLoadMode.AssetsRelative;
                    }

                    loader.RelativeModulePathFromAssets = normalizedPath;
                },
                out error);
        }

        public static bool PersistStreamingAssetsRelativeModulePathForPreviewScenes(
            string relativePathFromStreamingAssets,
            out string error)
        {
            string normalizedPath = NormalizeSerializedPath(relativePathFromStreamingAssets);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                error = "StreamingAssets-relative module path is empty.";
                return false;
            }

            return PersistLoaderPathsForScenes(
                PreviewScenePaths,
                (scenePath, loader) =>
                {
                    if (string.Equals(scenePath, RuntimePreviewHeadsetScenePath, StringComparison.OrdinalIgnoreCase))
                    {
                        loader.LoadMode = ModuleLoadMode.StreamingAssetsRelative;
                    }

                    loader.RelativeModulePathFromStreamingAssets = normalizedPath;
                },
                out error);
        }

        private static bool PersistLoaderPathsForScenes(
            string[] scenePaths,
            Action<string, RuntimeModuleLoader> applyToLoader,
            out string error)
        {
            error = "";

            if (scenePaths == null || scenePaths.Length == 0)
            {
                error = "No preview scenes were configured.";
                return false;
            }

            for (int i = 0; i < scenePaths.Length; i++)
            {
                if (!PersistLoaderPathsForScene(scenePaths[i], applyToLoader, out error))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PersistLoaderPathsForScene(
            string scenePath,
            Action<string, RuntimeModuleLoader> applyToLoader,
            out string error)
        {
            error = "";

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                error = "Preview scene path is empty.";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                error = "Preview scene asset was not found: " + scenePath;
                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedForEdit = !scene.IsValid() || !scene.isLoaded;

            try
            {
                if (openedForEdit)
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }

                RuntimeModuleLoader[] loaders = FindLoadersInScene(scene);
                if (loaders.Length == 0)
                {
                    error = "Preview scene does not contain a RuntimeModuleLoader: " + scenePath;
                    return false;
                }

                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Update runtime module loader defaults");

                for (int i = 0; i < loaders.Length; i++)
                {
                    RuntimeModuleLoader loader = loaders[i];
                    if (loader == null)
                    {
                        continue;
                    }

                    Undo.RecordObject(loader, "Update runtime module loader defaults");
                    applyToLoader(scenePath, loader);
                    EditorUtility.SetDirty(loader);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    error = "Could not save preview scene: " + scenePath;
                    return false;
                }

                return true;
            }
            finally
            {
                if (openedForEdit && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static RuntimeModuleLoader[] FindLoadersInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Array.Empty<RuntimeModuleLoader>();
            }

            GameObject[] roots = scene.GetRootGameObjects();
            int totalCount = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null)
                {
                    continue;
                }

                totalCount += roots[i].GetComponentsInChildren<RuntimeModuleLoader>(true).Length;
            }

            if (totalCount == 0)
            {
                return Array.Empty<RuntimeModuleLoader>();
            }

            RuntimeModuleLoader[] loaders = new RuntimeModuleLoader[totalCount];
            int insertIndex = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null)
                {
                    continue;
                }

                RuntimeModuleLoader[] rootLoaders = roots[i].GetComponentsInChildren<RuntimeModuleLoader>(true);
                for (int j = 0; j < rootLoaders.Length; j++)
                {
                    loaders[insertIndex] = rootLoaders[j];
                    insertIndex++;
                }
            }

            return loaders;
        }

        private static string NormalizeSerializedPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? "" : path.Replace("\\", "/");
        }
    }
}