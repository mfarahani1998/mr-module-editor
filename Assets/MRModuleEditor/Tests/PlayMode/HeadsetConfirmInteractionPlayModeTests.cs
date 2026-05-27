using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Interaction;
using MRModuleEditor.Runtime.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class HeadsetConfirmInteractionPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpatialConfirmPanel_SubmitsConfirmationFromInteractionSignal()
        {
            GameObject contextObject = new GameObject("Interaction Context");
            InteractionContext context = contextObject.AddComponent<InteractionContext>();

            GameObject panelObject = new GameObject("Spatial Confirm Panel");
            SpatialConfirmPanel panel = panelObject.AddComponent<SpatialConfirmPanel>();

            ModuleDocument module = new ModuleDocument();
            module.schemaVersion = "0.1";
            module.moduleId = "module.confirm.spatial";
            module.title = "Spatial Confirm";
            module.anchors.Add(new AnchorDefinition { id = "anchor.head.default", type = "head" });

            ModuleStep step = new ModuleStep
            {
                id = "step.confirm.spatial",
                type = "confirm",
                title = "Ready"
            };
            step.parameters["message"] = JToken.FromObject("Confirm this on headset.");
            module.steps.Add(step);

            yield return null;

            panel.ShowConfirm(module, step, "Confirm this on headset.", "Continue");

            yield return null;

            Assert.IsFalse(panel.HasConfirmation);
            Assert.AreEqual(1, context.ActiveTargetCount);

            bool emitted = context.TryEmitSelectByPayload(0, InteractionSource.Keyboard);
            Assert.IsTrue(emitted);

            yield return null;

            Assert.IsTrue(panel.HasConfirmation);

            Object.Destroy(panelObject);
            Object.Destroy(contextObject);
        }
    }
}
