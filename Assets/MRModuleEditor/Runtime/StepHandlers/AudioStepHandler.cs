using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Audio;
using MRModuleEditor.Runtime.IO;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class AudioStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "audio"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string assetId = step.GetString("assetId", "");
            string caption = step.GetString("caption", "");
            bool waitForCompletion = step.GetBool("waitForCompletion", true);
            bool loop = step.GetBool("loop", false);
            float volume = Mathf.Clamp01(step.GetFloat("volume", 1f));
            float spatialBlend = Mathf.Clamp01(step.GetFloat("spatialBlend", 0f));
            float captionDuration = StepParameterReader.GetDuration(step, 0f);

            string pathOrUrl;
            string error;
            if (!context.TryResolveAssetPath(assetId, out pathOrUrl, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null)
                {
                    context.LogError(error);
                }
                yield break;
            }

            AudioClip clip = null;
            string loadError = null;

            yield return RuntimeFileReader.LoadAudioClip(
                pathOrUrl,
                loadedClip => clip = loadedClip,
                audioError => loadError = audioError);

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (!string.IsNullOrEmpty(loadError) || clip == null)
            {
                if (context.LogError != null)
                {
                    context.LogError(loadError ?? "Could not load audio clip.");
                }
                yield break;
            }

            RuntimeAudioService audioService = GetOrCreateAudioService();
            if (audioService == null)
            {
                if (context.LogError != null)
                {
                    context.LogError("RuntimeAudioService could not be created.");
                }
                yield break;
            }

            AudioSource source = audioService.PlayClip(step.id, clip, loop, volume, spatialBlend);
            if (source == null)
            {
                if (context.LogError != null)
                {
                    context.LogError("AudioSource could not play clip for step '" + step.id + "'.");
                }
                yield break;
            }

            bool showedCaption = !string.IsNullOrWhiteSpace(caption);
            if (showedCaption)
            {
                if (context.DisplayPanel != null)
                {
                    context.DisplayPanel.ShowText(step.title, caption);
                }

                if (context.SpatialUI != null)
                {
                    context.SpatialUI.ShowText(context.Module, step, caption);
                }
            }

            if (waitForCompletion)
            {
                yield return WaitForSourceToFinish(source, audioService, context);
            }
            else if (captionDuration > 0f)
            {
                yield return context.WaitRespectingPause(captionDuration);
            }

            if (context.IsCancellationRequested)
            {
                if (waitForCompletion)
                {
                    audioService.StopSource(source);
                }
                yield break;
            }

            if (showedCaption && context.SpatialUI != null)
            {
                context.SpatialUI.ClearStep(step.id);
            }
        }

        private static IEnumerator WaitForSourceToFinish(
            AudioSource source,
            RuntimeAudioService audioService,
            RuntimeContext context)
        {
            bool pausedByHandler = false;

            while (source != null)
            {
                if (context.IsCancellationRequested)
                {
                    audioService.StopSource(source);
                    yield break;
                }

                bool paused = context.IsPaused != null && context.IsPaused();
                if (paused)
                {
                    if (!pausedByHandler && source.isPlaying)
                    {
                        source.Pause();
                        pausedByHandler = true;
                    }

                    yield return null;
                    continue;
                }

                if (pausedByHandler)
                {
                    source.UnPause();
                    pausedByHandler = false;
                }

                if (!source.isPlaying)
                {
                    break;
                }

                yield return null;
            }

            if (source != null && !source.loop)
            {
                audioService.StopSource(source);
            }
        }

        private static RuntimeAudioService GetOrCreateAudioService()
        {
            RuntimeAudioService service = Object.FindFirstObjectByType<RuntimeAudioService>(FindObjectsInactive.Include);
            if (service != null)
            {
                return service;
            }

            GameObject serviceObject = new GameObject("Runtime Audio Service");
            return serviceObject.AddComponent<RuntimeAudioService>();
        }
    }
}