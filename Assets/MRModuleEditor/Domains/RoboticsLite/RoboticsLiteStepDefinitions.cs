using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using MRModuleEditor.Core.Validation;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public static class RoboticsLiteStepDefinitions
    {
        public static void Register()
        {
            Register(StepCatalog.Global);
        }

        public static void Register(StepCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            catalog.Register(new StepTypeDefinition(
                "showFrame",
                "Show Frame",
                "Robotics Lite",
                "Shows or hides a simple joint frame gizmo on a RobotLite rig.",
                false,
                1f,
                new[]
                {
                    new StepParameterDefinition("objectId", "Object", StepParameterKind.ObjectId, true, "object.robot_preview"),
                    new StepParameterDefinition("jointIndex", "Joint Index", StepParameterKind.Int, false, 2),
                    new StepParameterDefinition("visible", "Visible", StepParameterKind.Bool, false, true)
                },
                ValidateJointIndex));

            catalog.Register(new StepTypeDefinition(
                "rotateJoint",
                "Rotate Joint",
                "Robotics Lite",
                "Rotates one joint on a RobotLite rig.",
                false,
                2f,
                new[]
                {
                    new StepParameterDefinition("objectId", "Object", StepParameterKind.ObjectId, true, "object.robot_preview"),
                    new StepParameterDefinition("jointIndex", "Joint Index", StepParameterKind.Int, false, 0),
                    new StepParameterDefinition("angleDegrees", "Angle Degrees", StepParameterKind.Float, false, 50f),
                    new StepParameterDefinition("showFrame", "Show Frame", StepParameterKind.Bool, false, true)
                },
                ValidateRotateJoint));

            catalog.Register(new StepTypeDefinition(
                "resetRobot",
                "Reset Robot",
                "Robotics Lite",
                "Returns the selected RobotLite rig to its captured home pose.",
                false,
                0.5f,
                new[]
                {
                    new StepParameterDefinition("objectId", "Object", StepParameterKind.ObjectId, true, "object.robot_preview")
                }));
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterForEditor()
        {
            Register();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterForRuntime()
        {
            Register();
        }

        private static void ValidateRotateJoint(ModuleStep step, StepValidationContext context, string location, List<ValidationIssue> issues)
        {
            ValidateJointIndex(step, context, location, issues);

            if (step.GetToken("angleDegrees") == null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "robotics.angleDegrees.missing",
                    "rotateJoint step is missing angleDegrees.",
                    location));
            }
        }

        private static void ValidateJointIndex(ModuleStep step, StepValidationContext context, string location, List<ValidationIssue> issues)
        {
            int jointIndex = step.GetInt("jointIndex", -1);
            if (jointIndex < 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "robotics.jointIndex.invalid",
                    "Robotics step must have a non-negative jointIndex.",
                    location));
            }
        }
    }
}
