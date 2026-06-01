using System;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Core.Layouts
{
    [Serializable]
    public class LayoutPresetDefinition
    {
        public string id = "";
        public string displayName = "";
        public string description = "";
        public string targetKind = "";
        public string anchorType = "";
        public Vector3Data position = new Vector3Data();
        public Vector3Data rotationEuler = new Vector3Data();
        public Vector3Data scale = new Vector3Data(1f, 1f, 1f);

        public LayoutPresetDefinition()
        {
        }

        public LayoutPresetDefinition(
            string id,
            string displayName,
            string description,
            string targetKind,
            string anchorType,
            Vector3Data position,
            Vector3Data rotationEuler,
            Vector3Data scale)
        {
            this.id = id ?? "";
            this.displayName = displayName ?? "";
            this.description = description ?? "";
            this.targetKind = targetKind ?? "";
            this.anchorType = anchorType ?? "";
            this.position = Clone(position, new Vector3Data());
            this.rotationEuler = Clone(rotationEuler, new Vector3Data());
            this.scale = Clone(scale, new Vector3Data(1f, 1f, 1f));
        }

        public LayoutPresetDefinition Clone()
        {
            return new LayoutPresetDefinition(
                id,
                displayName,
                description,
                targetKind,
                anchorType,
                position,
                rotationEuler,
                scale);
        }

        internal static Vector3Data Clone(Vector3Data source, Vector3Data fallback)
        {
            Vector3Data value = source ?? fallback ?? new Vector3Data();
            return new Vector3Data(value.x, value.y, value.z);
        }
    }
}
