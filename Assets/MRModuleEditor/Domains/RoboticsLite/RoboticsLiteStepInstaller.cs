using MRModuleEditor.Runtime;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    [DefaultExecutionOrder(-50)]
    public class RoboticsLiteStepInstaller : MonoBehaviour
    {
        [SerializeField]
        private ModuleRunner moduleRunner;

        private void Awake()
        {
            if (moduleRunner == null)
            {
                moduleRunner = FindFirstObjectByType<ModuleRunner>();
            }

            if (moduleRunner == null)
            {
                Debug.LogWarning("RoboticsLiteStepInstaller could not find a ModuleRunner.");
                return;
            }

            moduleRunner.RegisterStepHandler(new RotateJointStepHandler());
            moduleRunner.RegisterStepHandler(new ShowFrameStepHandler());
        }
    }
}