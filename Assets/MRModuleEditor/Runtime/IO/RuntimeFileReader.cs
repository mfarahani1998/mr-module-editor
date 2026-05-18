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
    }
}