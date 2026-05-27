using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Templates;
using MRModuleEditor.Core.Validation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class RoboticsLiteValidationTests
    {
        [Test]
        public void ForwardKinematicsTemplate_HasRoboticsLiteSteps()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();

            Assert.IsTrue(document.steps.Any(step => step.type == "showFrame"));
            Assert.IsTrue(document.steps.Any(step => step.type == "rotateJoint"));
            Assert.IsTrue(document.steps.Any(step => step.type == "resetRobot"));
        }

        [Test]
        public void RotateJointWithoutObjectId_ReportsError()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            ModuleStep rotate = document.steps.First(step => step.type == "rotateJoint");
            rotate.parameters.Remove("objectId");

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "step.objectId.missing"));
        }

        [Test]
        public void RotateJointWithInvalidJointIndex_ReportsError()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            ModuleStep rotate = document.steps.First(step => step.type == "rotateJoint");
            rotate.parameters["jointIndex"] = JToken.FromObject(-1);

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "robotics.jointIndex.invalid"));
        }

        [Test]
        public void RotateJointWithoutAngle_ReportsError()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            ModuleStep rotate = document.steps.First(step => step.type == "rotateJoint");
            rotate.parameters.Remove("angleDegrees");

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "robotics.angleDegrees.missing"));
        }

        [Test]
        public void ResetRobotWithoutObjectId_ReportsError()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            ModuleStep reset = document.steps.First(step => step.type == "resetRobot");
            reset.parameters.Remove("objectId");

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "step.objectId.missing"));
        }

        [Test]
        public void ResetRobotWithUnknownObjectId_ReportsError()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();
            ModuleStep reset = document.steps.First(step => step.type == "resetRobot");
            reset.parameters["objectId"] = JToken.FromObject("object.missing");

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "step.objectId.unknown"));
        }
    }
}