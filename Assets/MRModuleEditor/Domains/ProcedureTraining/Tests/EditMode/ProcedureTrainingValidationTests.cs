using System.IO;
using System.Linq;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using MRModuleEditor.Core.Validation;
using MRModuleEditor.Domains.ProcedureTraining;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MRModuleEditor.Domains.ProcedureTraining.Tests.EditMode
{
    public class ProcedureTrainingValidationTests
    {
        [Test]
        public void ProcedureTrainingDefinitions_RegisterCatalogEntries()
        {
            ProcedureTrainingStepDefinitions.Register();

            StepTypeDefinition showDefinition;
            StepTypeDefinition checkDefinition;

            Assert.IsTrue(StepCatalog.Global.TryGet("showProcedureItem", out showDefinition));
            Assert.AreEqual("Procedure Training", showDefinition.Category);

            Assert.IsTrue(StepCatalog.Global.TryGet("checkSafetyPoint", out checkDefinition));
            Assert.AreEqual("Procedure Training", checkDefinition.Category);
        }

        [Test]
        public void ProcedureTrainingSample_ValidatesWithoutCoreStepNameHardCoding()
        {
            ProcedureTrainingStepDefinitions.Register();

            ModuleDocument document = ProcedureTrainingModuleFactory.CreateEquipmentSafetyProcedureMini();
            var issues = ModuleValidator.Validate(document);

            Assert.IsFalse(
                issues.Any(issue => issue.severity == ValidationSeverity.Error),
                string.Join("\n", issues.Select(issue => issue.ToString()).ToArray()));
        }

        [Test]
        public void ShowProcedureItem_InvalidColor_ReportsDomainValidationError()
        {
            ProcedureTrainingStepDefinitions.Register();

            ModuleDocument document = ProcedureTrainingModuleFactory.CreateEquipmentSafetyProcedureMini();
            ModuleStep showStep = document.steps.First(step => step.type == "showProcedureItem");
            showStep.parameters["colorHex"] = JToken.FromObject("not-a-color");

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "procedureTraining.colorHex.invalid"));
        }

        [Test]
        public void CheckSafetyPoint_NegativeAutoComplete_ReportsDomainValidationError()
        {
            ProcedureTrainingStepDefinitions.Register();

            ModuleDocument document = ProcedureTrainingModuleFactory.CreateEquipmentSafetyProcedureMini();
            ModuleStep safetyStep = document.steps.First(step => step.type == "checkSafetyPoint");
            safetyStep.parameters["autoCompleteAfterSeconds"] = JToken.FromObject(-1f);

            var issues = ModuleValidator.Validate(document);

            Assert.IsTrue(issues.Any(issue => issue.code == "procedureTraining.autoCompleteAfterSeconds.negative"));
        }

        [Test]
        public void CoreValidator_DoesNotHardCodeProcedureTrainingStepTypeNames()
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "MRModuleEditor",
                "Core",
                "Validation",
                "ModuleValidator.cs");

            string source = File.ReadAllText(path);

            Assert.IsFalse(source.Contains("showProcedureItem"));
            Assert.IsFalse(source.Contains("checkSafetyPoint"));
        }
    }
}
