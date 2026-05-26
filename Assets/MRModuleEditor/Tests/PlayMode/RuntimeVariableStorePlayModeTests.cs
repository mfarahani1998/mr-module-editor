using System.Collections;
using MRModuleEditor.Runtime.Variables;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class RuntimeVariableStorePlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeVariableStore_SetString_BecomesReadableAfterFlush()
        {
            GameObject go = new GameObject("Runtime Variable Store Test");
            RuntimeVariableStore store = go.AddComponent<RuntimeVariableStore>();

            store.SetString("robot.joint3.angleText", "Joint 3: 50°");
            store.FlushPendingUpdates();

            string value;
            Assert.IsTrue(store.TryGetString("robot.joint3.angleText", out value));
            Assert.AreEqual("Joint 3: 50°", value);

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeVariableStore_Reset_ClearsValues()
        {
            GameObject go = new GameObject("Runtime Variable Store Test");
            RuntimeVariableStore store = go.AddComponent<RuntimeVariableStore>();

            store.SetString("status", "ready");
            store.FlushPendingUpdates();
            store.ResetRuntimeState();

            string value;
            Assert.IsFalse(store.TryGetString("status", out value));

            Object.Destroy(go);
            yield return null;
        }
    }
}