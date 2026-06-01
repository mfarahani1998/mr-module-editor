using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Templates;
using MRModuleEditor.Runtime.Anchors;
using MRModuleEditor.Runtime.SceneBinding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class Phase5AnchorStatusPlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeAnchorStatusUtility_ReportsResolvedObjectAnchor()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateEquipmentOrientationMini();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -2f);
            camera.transform.rotation = Quaternion.identity;

            GameObject services = new GameObject("Services");
            SceneBindingRegistry registry = services.AddComponent<SceneBindingRegistry>();
            AnchorResolver resolver = services.AddComponent<AnchorResolver>();

            GameObject equipment = new GameObject("Equipment Demo");
            BindableObject bindable = equipment.AddComponent<BindableObject>();
            bindable.BindingKey = "Equipment Demo";
            equipment.transform.position = new Vector3(0f, 0f, 1f);

            yield return null;
            registry.Rebuild();

            List<RuntimeAnchorStatus> statuses = RuntimeAnchorStatusUtility.Collect(document, resolver);
            RuntimeAnchorStatus objectAnchor = statuses.FirstOrDefault(status => status.anchorId == "anchor.object.equipment");

            Assert.That(objectAnchor, Is.Not.Null);
            Assert.That(objectAnchor.resolved, Is.True, objectAnchor.message);
            Assert.That(RuntimeAnchorStatusUtility.CountResolved(statuses), Is.GreaterThanOrEqualTo(3));

            Object.Destroy(equipment);
            Object.Destroy(services);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator RuntimeAnchorStatusUtility_ReportsFallbackWhenProviderIsMissing()
        {
            ModuleDocument document = new ModuleDocument
            {
                schemaVersion = "0.1",
                moduleId = "module.phase5.fallback",
                title = "Fallback Anchor Test"
            };

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.head.default",
                type = "head",
                provider = AnchorProviderIds.Simulator
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.world.manual",
                type = "world",
                provider = AnchorProviderIds.Manual,
                calibrationRequired = true,
                fallbackAnchorId = "anchor.head.default"
            });

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -2f);
            camera.transform.rotation = Quaternion.identity;

            GameObject services = new GameObject("Services");
            AnchorResolver resolver = services.AddComponent<AnchorResolver>();

            yield return null;

            List<RuntimeAnchorStatus> statuses = RuntimeAnchorStatusUtility.Collect(document, resolver);
            RuntimeAnchorStatus manualAnchor = statuses.FirstOrDefault(status => status.anchorId == "anchor.world.manual");

            Assert.That(manualAnchor, Is.Not.Null);
            Assert.That(manualAnchor.resolved, Is.True, manualAnchor.message);
            Assert.That(manualAnchor.usedFallback, Is.True);
            Assert.That(manualAnchor.state, Is.EqualTo(AnchorCalibrationStatuses.Approximate));

            Object.Destroy(services);
            Object.Destroy(cameraObject);
        }
    }
}
