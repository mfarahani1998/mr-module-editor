using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Domains.RoboticsLite;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.SceneBinding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Newtonsoft.Json.Linq;

namespace MRModuleEditor.Tests.PlayMode
{
    public class RoboticsLiteStepHandlerPlayModeTests
    {
        [UnityTest]
        public IEnumerator RotateJointStepHandler_RotatesRobotLiteJoint()
        {
            ModuleDocument document = new ModuleDocument();
            document.moduleId = "module.robotics_test";
            document.title = "Robotics Test";
            document.objects.Add(new ModuleObject
            {
                id = "object.robot_preview",
                label = "Robot Preview",
                bindingKey = "RobotPreview"
            });

            GameObject services = new GameObject("Services");
            SceneBindingRegistry registry = services.AddComponent<SceneBindingRegistry>();

            GameObject robot = new GameObject("RobotLiteArm");
            BindableObject bindable = robot.AddComponent<BindableObject>();
            bindable.BindingKey = "RobotPreview";
            RobotLiteRig rig = robot.AddComponent<RobotLiteRig>();

            GameObject joint = new GameObject("Joint0");
            joint.transform.SetParent(robot.transform, false);
            rig.ConfigureForTests(
                new[] { joint.transform },
                new[] { Vector3.up });

            yield return null;
            registry.Rebuild();

            ModuleStep step = new ModuleStep
            {
                id = "step.rotate",
                type = "rotateJoint",
                title = "Rotate",
                durationSeconds = 0.01f
            };
            step.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            step.parameters["jointIndex"] = JToken.FromObject(0);
            step.parameters["angleDegrees"] = JToken.FromObject(45f);
            step.parameters["showFrame"] = JToken.FromObject(false);

            RuntimeContext context = new RuntimeContext(
                document,
                "",
                registry,
                null,
                null,
                null,
                () => false,
                () => false,
                message => Debug.Log(message),
                message => Debug.LogError(message));

            RotateJointStepHandler handler = new RotateJointStepHandler();
            yield return handler.Execute(step, context);

            Assert.AreEqual(45f, rig.GetJointAngle(0), 0.1f);

            Object.Destroy(robot);
            Object.Destroy(services);
        }
    }
}