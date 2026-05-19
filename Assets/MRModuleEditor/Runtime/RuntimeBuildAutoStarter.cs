using System.Collections;
using UnityEngine;
using MRModuleEditor.Runtime.Anchors;

namespace MRModuleEditor.Runtime
{
    public class RuntimeBuildAutoStarter : MonoBehaviour
    {
        [SerializeField]
        private RuntimeModuleLoader moduleLoader;

        [SerializeField]
        private ModuleRunner moduleRunner;

        [SerializeField]
        private bool playAfterLoad = true;

        [SerializeField]
        private float delayBeforePlaySeconds = 0.25f;

        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private bool recenterWorldBeforePlay = true;

        [SerializeField]
        private int framesBeforeRecenter = 2;

        private IEnumerator Start()
        {
            Debug.Log("[MRModule] RuntimeBuildAutoStarter.Start entered. streamingAssetsPath="
                + Application.streamingAssetsPath);

            if (moduleLoader == null)
            {
                moduleLoader = FindFirstObjectByType<RuntimeModuleLoader>();
            }

            if (moduleRunner == null)
            {
                moduleRunner = FindFirstObjectByType<ModuleRunner>();
            }

            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            Debug.Log("[MRModule] AutoStarter refs: loader=" + (moduleLoader != null)
                + " runner=" + (moduleRunner != null)
                + " anchorResolver=" + (anchorResolver != null));

            if (moduleLoader == null || moduleRunner == null)
            {
                Debug.LogError("[MRModule] RuntimeBuildAutoStarter needs both RuntimeModuleLoader and ModuleRunner.");
                yield break;
            }

            bool loadSucceeded = false;
            yield return moduleLoader.LoadAndValidateAsync(success => loadSucceeded = success);

            Debug.Log("[MRModule] LoadAndValidateAsync finished. success=" + loadSucceeded
                + " path=" + moduleLoader.LastLoadedPathOrUrl
                + " issueCount=" + (moduleLoader.LastIssues == null ? 0 : moduleLoader.LastIssues.Count));

            if (!loadSucceeded)
            {
                Debug.LogError("[MRModule] RuntimeBuildAutoStarter stopped because the module failed to load.");
                yield break;
            }

            for (int i = 0; i < framesBeforeRecenter; i++)
            {
                yield return null;
            }

            if (recenterWorldBeforePlay && anchorResolver != null)
            {
                Debug.Log("[MRModule] Recentering simulator world origin.");
                anchorResolver.RecenterSimulatorWorldOrigin();
            }

            bool runnerLoaded = moduleRunner.LoadModule();

            Debug.Log("[MRModule] ModuleRunner.LoadModule returned " + runnerLoaded
                + " state=" + moduleRunner.State
                + " module=" + moduleRunner.CurrentModuleTitle);

            if (!runnerLoaded)
            {
                Debug.LogError("[MRModule] RuntimeBuildAutoStarter stopped because ModuleRunner.LoadModule failed.");
                yield break;
            }

            if (delayBeforePlaySeconds > 0f)
            {
                yield return new WaitForSeconds(delayBeforePlaySeconds);
            }

            if (playAfterLoad)
            {
                Debug.Log("[MRModule] Calling ModuleRunner.Play().");
                moduleRunner.Play();
            }
        }
    }
}