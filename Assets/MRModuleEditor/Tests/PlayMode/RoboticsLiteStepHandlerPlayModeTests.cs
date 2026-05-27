using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Domains.RoboticsLite;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.SceneBinding;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class RoboticsLiteStepHandlerPlayModeTests
    {
        [UnityTest]
        public IEnumerator RotateJointStepHandler_RotatesRobotLiteJoint()
        {
            ModuleDocument document;
            SceneBindingRegistry registry;
            RobotLiteRig rig;
            GameObject services;
            GameObject robot;
            CreateRobotTestScene(out document, out registry, out rig, out services, out robot);

            yield return null;
            registry.Rebuild();

            ModuleStep step = CreateRotateStep(45f, 0.01f);

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

        [UnityTest]
        public IEnumerator RotateJointStepHandler_CancelledExecutionDoesNotReachTargetAfterReset()
        {
            ModuleDocument document;
            SceneBindingRegistry registry;
            RobotLiteRig rig;
            GameObject services;
            GameObject robot;
            CreateRobotTestScene(out document, out registry, out rig, out services, out robot);

            GameObject hostObject = new GameObject("Coroutine Host");
            TestCoroutineHost host = hostObject.AddComponent<TestCoroutineHost>();

            yield return null;
            registry.Rebuild();

            ModuleStep step = CreateRotateStep(90f, 1f);
            RuntimeExecutionToken token = new RuntimeExecutionToken(1);

            RuntimeContext context = new RuntimeContext(
                document,
                "",
                registry,
                null,
                null,
                null,
                token,
                () => false,
                () => token.IsCancellationRequested,
                message => Debug.Log(message),
                message => Debug.LogError(message));

            RotateJointStepHandler handler = new RotateJointStepHandler();
            host.StartCoroutine(handler.Execute(step, context));

            yield return null;

            token.Cancel("Simulated restart.");
            rig.ResetRig();

            yield return null;
            yield return null;

            Assert.AreEqual(0f, rig.GetJointAngle(0), 0.1f, "Cancelled rotateJoint coroutine should not continue to the target after reset.");

            Object.Destroy(hostObject);
            Object.Destroy(robot);
            Object.Destroy(services);
        }

        [UnityTest]
        public IEnumerator ResetRobotStepHandler_ReturnsJointToHomePose()
        {
            ModuleDocument document;
            SceneBindingRegistry registry;
            RobotLiteRig rig;
            GameObject services;
            GameObject robot;
            CreateRobotTestScene(out document, out registry, out rig, out services, out robot);

            yield return null;
            registry.Rebuild();

            string error;
            Assert.IsTrue(rig.TrySetJointAngle(0, 45f, out error), error);
            Assert.AreEqual(45f, rig.GetJointAngle(0), 0.1f);

            ModuleStep step = CreateResetStep(0f);

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

            ResetRobotStepHandler handler = new ResetRobotStepHandler();
            yield return handler.Execute(step, context);

            Assert.AreEqual(0f, rig.GetJointAngle(0), 0.1f);

            Object.Destroy(robot);
            Object.Destroy(services);
        }

        [UnityTest]
        public IEnumerator ResetRobotStepHandler_MissingRig_LogsErrorAndDoesNotThrow()
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

            GameObject robot = new GameObject("RobotWithoutRig");
            BindableObject bindable = robot.AddComponent<BindableObject>();
            bindable.BindingKey = "RobotPreview";

            yield return null;
            registry.Rebuild();

            string loggedError = "";
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
                message => loggedError = message);

            ResetRobotStepHandler handler = new ResetRobotStepHandler();
            yield return handler.Execute(CreateResetStep(0f), context);

            Assert.IsTrue(loggedError.Contains("RobotLiteRig"));

            Object.Destroy(robot);
            Object.Destroy(services);
        }

        private static void CreateRobotTestScene(
            out ModuleDocument document,
            out SceneBindingRegistry registry,
            out RobotLiteRig rig,
            out GameObject services,
            out GameObject robot)
        {
            document = new ModuleDocument();
            document.moduleId = "module.robotics_test";
            document.title = "Robotics Test";
            document.objects.Add(new ModuleObject
            {
                id = "object.robot_preview",
                label = "Robot Preview",
                bindingKey = "RobotPreview"
            });

            services = new GameObject("Services");
            registry = services.AddComponent<SceneBindingRegistry>();

            robot = new GameObject("RobotLiteArm");
            BindableObject bindable = robot.AddComponent<BindableObject>();
            bindable.BindingKey = "RobotPreview";
            rig = robot.AddComponent<RobotLiteRig>();

            GameObject joint = new GameObject("Joint0");
            joint.transform.SetParent(robot.transform, false);
            rig.ConfigureForTests(
                new[] { joint.transform },
                new[] { Vector3.up });
        }

        private static ModuleStep CreateRotateStep(float angleDegrees, float durationSeconds)
        {
            ModuleStep step = new ModuleStep
            {
                id = "step.rotate",
                type = "rotateJoint",
                title = "Rotate",
                durationSeconds = durationSeconds
            };
            step.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            step.parameters["jointIndex"] = JToken.FromObject(0);
            step.parameters["angleDegrees"] = JToken.FromObject(angleDegrees);
            step.parameters["showFrame"] = JToken.FromObject(false);
            return step;
        }

        private static ModuleStep CreateResetStep(float durationSeconds)
        {
            ModuleStep step = new ModuleStep
            {
                id = "step.reset",
                type = "resetRobot",
                title = "Reset",
                durationSeconds = durationSeconds
            };
            step.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            return step;
        }

        private sealed class TestCoroutineHost : MonoBehaviour
        {
        }
    }
}
