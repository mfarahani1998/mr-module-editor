using System.Collections;
using MRModuleEditor.Runtime.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class RuntimeAudioServicePlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeAudioService_PlayClip_CreatesAudioSource()
        {
            GameObject go = new GameObject("Runtime Audio Service Test");
            RuntimeAudioService service = go.AddComponent<RuntimeAudioService>();
            AudioClip clip = AudioClip.Create("silent", 4410, 1, 44100, false);

            AudioSource source = service.PlayClip("test", clip, false, 1f);

            Assert.IsNotNull(source);
            Assert.AreEqual(clip, source.clip);

            service.ResetRuntimeState();
            Object.Destroy(go);
            yield return null;
        }
    }
}