using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public interface IAnchorProvider
    {
        string ProviderId { get; }

        bool TryResolveAnchor(
            ModuleDocument module,
            AnchorDefinition anchor,
            AnchorResolver resolver,
            out Pose pose,
            out string error);
    }
}
