using System.IO;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class ModuleJsonSerializerTests
    {
        private static string SamplePath
        {
            get
            {
                return Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Assets",
                    ModuleFilePaths.SampleModuleRelativePathFromAssets);
            }
        }

        [Test]
        public void SampleModuleFileExists()
        {
            Assert.IsTrue(File.Exists(SamplePath), "Missing sample file: " + SamplePath);
        }

        [Test]
        public void LoadSampleModule_ReturnsExpectedTitleAndSteps()
        {
            ModuleDocument document = ModuleJsonSerializer.LoadFromFile(SamplePath);

            Assert.AreEqual("0.1", document.schemaVersion);
            Assert.AreEqual("module.forward_kinematics_mini", document.moduleId);
            Assert.AreEqual("Forward Kinematics Mini Demo", document.title);
            Assert.AreEqual(5, document.steps.Count);
            Assert.AreEqual("text", document.steps[0].type);
            Assert.AreEqual("showObject", document.steps[2].type);
            Assert.AreEqual("mcq", document.steps[4].type);
        }

        [Test]
        public void LoadSampleModule_ReadsFlexibleParameters()
        {
            ModuleDocument document = ModuleJsonSerializer.LoadFromFile(SamplePath);
            ModuleStep firstStep = document.steps[0];
            ModuleStep mcqStep = document.steps[4];

            Assert.AreEqual(
                "Welcome to the forward kinematics mini demo.",
                firstStep.GetString("text"));

            JArray choices = mcqStep.GetToken("choices") as JArray;
            Assert.IsNotNull(choices);
            Assert.AreEqual(4, choices.Count);
            Assert.AreEqual(1, mcqStep.GetInt("correctIndex", -1));
        }

        [Test]
        public void RoundTrip_PreservesStepTypeAndParameters()
        {
            ModuleDocument original = ModuleJsonSerializer.LoadFromFile(SamplePath);
            string json = ModuleJsonSerializer.Serialize(original);
            ModuleDocument copy = ModuleJsonSerializer.Deserialize(json);

            Assert.AreEqual(original.title, copy.title);
            Assert.AreEqual(original.steps.Count, copy.steps.Count);
            Assert.AreEqual(original.steps[4].type, copy.steps[4].type);
            Assert.AreEqual(
                "What does forward kinematics compute?",
                copy.steps[4].GetString("question"));
        }
    }
}