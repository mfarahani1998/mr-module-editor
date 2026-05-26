using System.Collections.Generic;
using UnityEngine;

namespace MRModuleEditor.Runtime.Audio
{
    public class RuntimeAudioService : MonoBehaviour, IRuntimeResettable
    {
        private readonly List<AudioSource> activeSources = new List<AudioSource>();

        public AudioSource PlayClip(
            string playbackId,
            AudioClip clip,
            bool loop,
            float volume,
            float spatialBlend = 0f)
        {
            if (clip == null)
            {
                return null;
            }

            GameObject sourceObject = new GameObject(MakeSourceName(playbackId));
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = loop;
            source.volume = Mathf.Clamp01(volume);
            source.spatialBlend = Mathf.Clamp01(spatialBlend);
            source.Play();

            activeSources.Add(source);
            return source;
        }

        public bool IsPlaying(AudioSource source)
        {
            return source != null && source.isPlaying;
        }

        public void StopSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            activeSources.Remove(source);
            source.Stop();

            if (source.gameObject != null)
            {
                Destroy(source.gameObject);
            }
        }

        public void StopAll()
        {
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                AudioSource source = activeSources[i];
                if (source == null)
                {
                    continue;
                }

                source.Stop();
                if (source.gameObject != null)
                {
                    Destroy(source.gameObject);
                }
            }

            activeSources.Clear();
        }

        public void ResetRuntimeState()
        {
            StopAll();
        }

        private static string MakeSourceName(string playbackId)
        {
            return string.IsNullOrWhiteSpace(playbackId)
                ? "Runtime Audio"
                : "Runtime Audio - " + playbackId;
        }
    }
}