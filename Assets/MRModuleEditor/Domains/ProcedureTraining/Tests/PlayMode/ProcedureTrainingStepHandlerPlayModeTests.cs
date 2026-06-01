using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Domains.ProcedureTraining;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.ObjectState;
using MRModuleEditor.Runtime.SceneBinding;
using MRModuleEditor.Runtime.Variables;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Domains.ProcedureTraining.Tests.PlayMode
{
    public class ProcedureTrainingStepHandlerPlayModeTests
    {
        [UnityTest]
        public IEnumerator ShowProcedureItemStepHandler_ShowsObjectHighlightsAndRecordsCurrentItem()
        {
            ModuleDocument document;
            SceneBindingRegistry registry;
            RuntimeVariableStore variables;
            GameObject services;
            GameObject target;
            CreateProcedureTestScene(out document, out registry, out variables, out services, out target);

            yield return null;
            registry.Rebuild();
            target.SetActive(false);

            RuntimeContext context = MakeContext(document, registry, variables);
            ModuleStep step = CreateShowProcedureItemStep();

            yield return new ShowProcedureItemStepHandler().Execute(step, context);

            Assert.IsTrue(target.activeSelf);
            Assert.IsNotNull(target.GetComponent<ObjectHighlightController>());

            ProcedureItemMarker marker = target.GetComponent<ProcedureItemMarker>();
            Assert.IsNotNull(marker);
            Assert.AreEqual("guard-door", marker.ItemId);
            Assert.AreEqual("guard-door", variables.GetString("procedure.currentItem", ""));

            bool shown;
            Assert.IsTrue(context.Results.TryGetStepBool("step.show", "shown", out shown));
            Assert.IsTrue(shown);

            Object.Destroy(target);
            Object.Destroy(services);
        }

        [UnityTest]
        public IEnumerator CheckSafetyPointStepHandler_AutoCompletesAndStoresSafetyStatus()
        {
            ModuleDocument document;
            SceneBindingRegistry registry;
            RuntimeVariableStore variables;
            GameObject services;
            GameObject target;
            CreateProcedureTestScene(out document, out registry, out variables, out services, out target);

            yield return null;
            registry.Rebuild();

            RuntimeContext context = MakeContext(document, registry, variables);
            ModuleStep step = CreateCheckSafetyPointStep();

            yield return new CheckSafetyPointStepHandler().Execute(step, context);

            ProcedureItemMarker marker = target.GetComponent<ProcedureItemMarker>();
            Assert.IsNotNull(marker);
            Assert.IsTrue(marker.Completed);
            Assert.AreEqual("guard-door-closed", marker.LastSafetyPointId);
            Assert.AreEqual("Checked", marker.LastStatus);
            Assert.AreEqual("Checked", variables.GetString("procedure.guardDoor.status", ""));

            bool checkedValue;
            Assert.IsTrue(context.Results.TryGetStepBool("step.safety", "checked", out checkedValue));
            Assert.IsTrue(checkedValue);

            Object.Destroy(target);
            Object.Destroy(services);
        }

        private static RuntimeContext MakeContext(ModuleDocument document, SceneBindingRegistry registry, RuntimeVariableStore variables)
        {
            return new RuntimeContext(
                document,
                "",
                registry,
                null,
                null,
                null,
                new RuntimeExecutionToken(1),
                () => false,
                () => false,
                message => Debug.Log(message),
                message => Debug.LogError(message),
                null,
                variables);
        }

        private static void CreateProcedureTestScene(
            out ModuleDocument document,
            out SceneBindingRegistry registry,
            out RuntimeVariableStore variables,
            out GameObject services,
            out GameObject target)
        {
            document = ProcedureTrainingModuleFactory.CreateEquipmentSafetyProcedureMini();

            services = new GameObject("ProcedureTraining Test Services");
            registry = services.AddComponent<SceneBindingRegistry>();
            variables = services.AddComponent<RuntimeVariableStore>();

            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Equipment Demo";
            target.AddComponent<ProcedureItemMarker>();

            BindableObject bindable = target.AddComponent<BindableObject>();
            bindable.BindingKey = "Equipment Demo";
        }

        private static ModuleStep CreateShowProcedureItemStep()
        {
            ModuleStep step = new ModuleStep
            {
                id = "step.show",
                type = "showProcedureItem",
                title = "Show Procedure Item",
                durationSeconds = 0f
            };
            step.parameters["objectId"] = JToken.FromObject("object.equipment_demo");
            step.parameters["itemId"] = JToken.FromObject("guard-door");
            step.parameters["instruction"] = JToken.FromObject("Find the guard door.");
            step.parameters["highlight"] = JToken.FromObject(true);
            step.parameters["colorHex"] = JToken.FromObject("#FFD54F");
            step.parameters["pulseAmplitude"] = JToken.FromObject(0.01f);
            step.parameters["pulseSeconds"] = JToken.FromObject(0.8f);
            step.parameters["resultVariableKey"] = JToken.FromObject("procedure.currentItem");
            return step;
        }

        private static ModuleStep CreateCheckSafetyPointStep()
        {
            ModuleStep step = new ModuleStep
            {
                id = "step.safety",
                type = "checkSafetyPoint",
                title = "Check Safety Point",
                durationSeconds = 0f
            };
            step.parameters["objectId"] = JToken.FromObject("object.equipment_demo");
            step.parameters["safetyPointId"] = JToken.FromObject("guard-door-closed");
            step.parameters["prompt"] = JToken.FromObject("Confirm the guard door is closed.");
            step.parameters["buttonLabel"] = JToken.FromObject("Checked");
            step.parameters["status"] = JToken.FromObject("Checked");
            step.parameters["autoCompleteAfterSeconds"] = JToken.FromObject(0.01f);
            step.parameters["resultVariableKey"] = JToken.FromObject("procedure.guardDoor.status");
            step.parameters["highlightOnComplete"] = JToken.FromObject(true);
            step.parameters["highlightColorHex"] = JToken.FromObject("#66BB6A");
            return step;
        }
    }
}
