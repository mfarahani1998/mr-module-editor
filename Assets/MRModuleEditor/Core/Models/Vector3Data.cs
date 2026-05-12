using System;

namespace MRModuleEditor.Core.Models
{
    [Serializable]
    public class Vector3Data
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;

        public Vector3Data() { }

        public Vector3Data(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}