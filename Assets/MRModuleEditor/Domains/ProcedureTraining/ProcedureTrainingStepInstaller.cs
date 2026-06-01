using MRModuleEditor.Runtime;
using UnityEngine;

namespace MRModuleEditor.Domains.ProcedureTraining
{
    [DefaultExecutionOrder(-50)]
    public class ProcedureTrainingStepInstaller : MonoBehaviour
    {
        [SerializeField]
        private ModuleRunner moduleRunner;

        private void Awake()
        {
            ProcedureTrainingStepDefinitions.Register();

            if (moduleRunner == null)
            {
                moduleRunner = FindFirstObjectByType<ModuleRunner>();
            }

            if (moduleRunner == null)
            {
                Debug.LogWarning("ProcedureTrainingStepInstaller could not find a ModuleRunner.");
                return;
            }

            moduleRunner.RegisterStepHandler(new ShowProcedureItemStepHandler());
            moduleRunner.RegisterStepHandler(new CheckSafetyPointStepHandler());
        }
    }
}
