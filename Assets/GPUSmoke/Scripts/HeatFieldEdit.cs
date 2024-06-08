using System;
using UnityEngine;

namespace GPUSmoke
{
    public struct HeatFieldEdit : IStruct<float>
    {
        public Vector3 center;
        public float temp;

        public HeatFieldEdit(Vector3 center, float temp)
        {
            this.center = center;
            this.temp = temp;
        }

        public readonly int WordCount { get => 4; }
        public readonly void ToWords(Span<float> dst)
        {
            dst[0] = center.x;
            dst[1] = center.y;
            dst[2] = center.z;
            dst[3] = temp;
        }
    }

}