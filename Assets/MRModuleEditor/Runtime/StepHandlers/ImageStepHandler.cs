using System.Collections;
using System.IO;
using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class ImageStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "image"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            string assetId = step.GetString("assetId", "");
            string caption = step.GetString("caption", "");
            float duration = StepParameterReader.GetDuration(step, 3f);

            string absolutePath;
            string error;
            if (!context.TryResolveAssetPath(assetId, out absolutePath, out error))
            {
                if (context.LogError != null) context.LogError(error);
                yield break;
            }

            byte[] bytes = File.ReadAllBytes(absolutePath);
            Texture2D texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(bytes);

            if (!loaded)
            {
                if (context.LogError != null) context.LogError("Could not decode image: " + absolutePath);
                yield break;
            }

            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowImage(step.title, caption, texture);
            }

            yield return context.WaitRespectingPause(duration);
        }
    }
}