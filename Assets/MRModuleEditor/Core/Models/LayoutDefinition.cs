using System;

namespace MRModuleEditor.Core.Models
{
    [Serializable]
    public class LayoutDefinition
    {
        public string id = "";
        public string targetId = ""; // step id, object id, or panel id later
        public string anchorId = "";
        public Vector3Data position = new Vector3Data();
        public Vector3Data rotationEuler = new Vector3Data();
        public Vector3Data scale = new Vector3Data(1f, 1f, 1f);
    }
}