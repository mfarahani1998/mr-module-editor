using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MRModuleEditor.Runtime.IO
{
    public static class RuntimeFileReader
    {
        public static System.Collections.IEnumerator ReadText(
            string pathOrUrl,
            Action<string> onLoaded,
            Action<string> onError)
        {
            if (RuntimePathUtility.RequiresUnityWebRequest(pathOrUrl))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(pathOrUrl))
                {
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onError?.Invoke("Could not read text file: " + pathOrUrl + "\n" + request.error);
                        yield break;
                    }

                    onLoaded?.Invoke(request.downloadHandler.text);
                }
            }
            else
            {
                if (!File.Exists(pathOrUrl))
                {
                    onError?.Invoke("File not found: " + pathOrUrl);
                    yield break;
                }

                onLoaded?.Invoke(File.ReadAllText(pathOrUrl));
            }
        }

        public static System.Collections.IEnumerator ReadBytes(
            string pathOrUrl,
            Action<byte[]> onLoaded,
            Action<string> onError)
        {
            if (RuntimePathUtility.RequiresUnityWebRequest(pathOrUrl))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(pathOrUrl))
                {
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onError?.Invoke("Could not read binary file: " + pathOrUrl + "\n" + request.error);
                        yield break;
                    }

                    onLoaded?.Invoke(request.downloadHandler.data);
                }
            }
            else
            {
                if (!File.Exists(pathOrUrl))
                {
                    onError?.Invoke("File not found: " + pathOrUrl);
                    yield break;
                }

                onLoaded?.Invoke(File.ReadAllBytes(pathOrUrl));
            }
        }

        public static System.Collections.IEnumerator LoadTexture2D(
            string pathOrUrl,
            Action<Texture2D> onLoaded,
            Action<string> onError)
        {
            if (RuntimePathUtility.RequiresUnityWebRequest(pathOrUrl))
            {
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(pathOrUrl))
                {
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onError?.Invoke("Could not load texture: " + pathOrUrl + "\n" + request.error);
                        yield break;
                    }

                    onLoaded?.Invoke(DownloadHandlerTexture.GetContent(request));
                }
            }
            else
            {
                if (!File.Exists(pathOrUrl))
                {
                    onError?.Invoke("Image file not found: " + pathOrUrl);
                    yield break;
                }

                byte[] bytes = File.ReadAllBytes(pathOrUrl);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                bool loaded = texture.LoadImage(bytes);

                if (!loaded)
                {
                    UnityEngine.Object.Destroy(texture);
                    onError?.Invoke("Could not decode image: " + pathOrUrl);
                    yield break;
                }

                onLoaded?.Invoke(texture);
            }
        }

        public static System.Collections.IEnumerator LoadAudioClip(
            string pathOrUrl,
            Action<AudioClip> onLoaded,
            Action<string> onError)
        {
            string requestUrl = ToAudioRequestUrl(pathOrUrl);
            AudioType audioType = GuessAudioType(pathOrUrl);

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(requestUrl, audioType))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke("Could not load audio clip: " + pathOrUrl + "\n" + request.error);
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    onError?.Invoke("Unity loaded no AudioClip from: " + pathOrUrl);
                    yield break;
                }

                onLoaded?.Invoke(clip);
            }
        }

        private static string ToAudioRequestUrl(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                return "";
            }

            if (RuntimePathUtility.RequiresUnityWebRequest(pathOrUrl))
            {
                return pathOrUrl;
            }

            return new Uri(pathOrUrl).AbsoluteUri;
        }

        private static AudioType GuessAudioType(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                return AudioType.UNKNOWN;
            }

            string lower = pathOrUrl.ToLowerInvariant();
            if (lower.EndsWith(".wav")) return AudioType.WAV;
            if (lower.EndsWith(".ogg")) return AudioType.OGGVORBIS;
            if (lower.EndsWith(".mp3")) return AudioType.MPEG;
            if (lower.EndsWith(".aif") || lower.EndsWith(".aiff")) return AudioType.AIFF;

            return AudioType.UNKNOWN;
        }
    }
}