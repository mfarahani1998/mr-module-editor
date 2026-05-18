using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.IO;
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
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string assetId = step.GetString("assetId", "");
            string caption = step.GetString("caption", "");
            float duration = StepParameterReader.GetDuration(step, 3f);

            string pathOrUrl;
            string error;
            if (!context.TryResolveAssetPath(assetId, out pathOrUrl, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null)
                {
                    context.LogError(error);
                }
                yield break;
            }

            Texture2D texture = null;
            string loadError = null;

            yield return RuntimeFileReader.LoadTexture2D(
                pathOrUrl,
                loadedTexture => texture = loadedTexture,
                textureError => loadError = textureError);

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (!string.IsNullOrEmpty(loadError) || texture == null)
            {
                if (context.LogError != null)
                {
                    context.LogError(loadError ?? "Could not load image texture.");
                }
                yield break;
            }

            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowImage(step.title, caption, texture);
            }

            if (context.SpatialImagePanel != null)
            {
                context.SpatialImagePanel.ShowImage(context.Module, step, texture, caption);
            }
            else if (context.SpatialTextPanel != null)
            {
                string spatialText = string.IsNullOrWhiteSpace(caption) ? step.title : caption;
                context.SpatialTextPanel.ShowText(context.Module, step, spatialText);
            }

            yield return context.WaitRespectingPause(duration);

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (context.SpatialImagePanel != null)
            {
                context.SpatialImagePanel.ClearIfShowingStep(step.id);
            }

            if (context.SpatialTextPanel != null)
            {
                context.SpatialTextPanel.ClearIfShowingStep(step.id);
            }
        }
    }
}