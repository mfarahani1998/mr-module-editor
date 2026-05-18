using System.Collections;
using UnityEngine;

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

        private IEnumerator Start()
        {
            if (moduleLoader == null)
            {
                moduleLoader = FindFirstObjectByType<RuntimeModuleLoader>();
            }

            if (moduleRunner == null)
            {
                moduleRunner = FindFirstObjectByType<ModuleRunner>();
            }

            if (moduleLoader == null || moduleRunner == null)
            {
                Debug.LogError("RuntimeBuildAutoStarter needs both RuntimeModuleLoader and ModuleRunner.");
                yield break;
            }

            bool loadSucceeded = false;
            yield return moduleLoader.LoadAndValidateAsync(success => loadSucceeded = success);

            if (!loadSucceeded)
            {
                Debug.LogError("RuntimeBuildAutoStarter stopped because the module failed to load.");
                yield break;
            }

            bool runnerLoaded = moduleRunner.LoadModule();
            if (!runnerLoaded)
            {
                Debug.LogError("RuntimeBuildAutoStarter stopped because ModuleRunner.LoadModule failed.");
                yield break;
            }

            if (delayBeforePlaySeconds > 0f)
            {
                yield return new WaitForSeconds(delayBeforePlaySeconds);
            }

            if (playAfterLoad)
            {
                moduleRunner.Play();
            }
        }
    }
}