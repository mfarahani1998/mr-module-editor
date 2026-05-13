using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.SceneBinding;
using MRModuleEditor.Runtime.UI;
using UnityEngine;

namespace MRModuleEditor.Runtime
{
    public class RuntimeContext
    {
        private readonly Dictionary<string, ModuleAsset> assetsById = new Dictionary<string, ModuleAsset>();

        public RuntimeContext(
            ModuleDocument module,
            string moduleDirectory,
            SceneBindingRegistry sceneBindings,
            RuntimeDisplayPanel displayPanel,
            Func<bool> isPaused,
            Func<bool> stopRequested,
            Action<string> logInfo,
            Action<string> logError)
        {
            Module = module;
            ModuleDirectory = moduleDirectory ?? "";
            SceneBindings = sceneBindings;
            DisplayPanel = displayPanel;
            IsPaused = isPaused;
            StopRequested = stopRequested;
            LogInfo = logInfo;
            LogError = logError;

            IndexAssets(module);
        }

        public ModuleDocument Module { get; private set; }
        public string ModuleDirectory { get; private set; }
        public SceneBindingRegistry SceneBindings { get; private set; }
        public RuntimeDisplayPanel DisplayPanel { get; private set; }
        public Func<bool> IsPaused { get; private set; }
        public Func<bool> StopRequested { get; private set; }
        public Action<string> LogInfo { get; private set; }
        public Action<string> LogError { get; private set; }

        public bool TryResolveObject(string objectId, out GameObject result, out string error)
        {
            result = null;
            error = "";

            if (SceneBindings == null)
            {
                error = "SceneBindingRegistry is missing.";
                return false;
            }

            return SceneBindings.TryGetObjectByModuleObjectId(Module, objectId, out result, out error);
        }

        public bool TryResolveAssetPath(string assetId, out string absolutePath, out string error)
        {
            absolutePath = "";
            error = "";

            if (string.IsNullOrWhiteSpace(assetId))
            {
                error = "assetId is empty.";
                return false;
            }

            ModuleAsset asset;
            if (!assetsById.TryGetValue(assetId, out asset) || asset == null)
            {
                error = "No ModuleAsset with id '" + assetId + "' exists in the module.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(asset.path))
            {
                error = "Asset '" + assetId + "' has an empty path.";
                return false;
            }

            if (Path.IsPathRooted(asset.path))
            {
                absolutePath = asset.path;
            }
            else
            {
                absolutePath = Path.Combine(ModuleDirectory, asset.path);
            }

            if (!File.Exists(absolutePath))
            {
                error = "Asset file not found: " + absolutePath;
                return false;
            }

            return true;
        }

        public IEnumerator WaitRespectingPause(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (StopRequested != null && StopRequested())
                {
                    yield break;
                }

                if (IsPaused != null && IsPaused())
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator WaitUntilNotPausedOrStopped()
        {
            while (IsPaused != null && IsPaused())
            {
                if (StopRequested != null && StopRequested())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private void IndexAssets(ModuleDocument module)
        {
            assetsById.Clear();

            if (module == null || module.assets == null)
            {
                return;
            }

            for (int i = 0; i < module.assets.Count; i++)
            {
                ModuleAsset asset = module.assets[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.id))
                {
                    continue;
                }

                if (!assetsById.ContainsKey(asset.id))
                {
                    assetsById.Add(asset.id, asset);
                }
            }
        }
    }
}