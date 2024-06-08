using System;
using GPUSmoke;
using UnityEngine;

namespace GPUSmoke
{
    public struct HeatFieldEdit : IStruct<float>
    {
        public Vector3 center;
        public float radius, temp;

        public HeatFieldEdit(Vector3 center, float radius, float temp)
        {
            this.center = center;
            this.radius = radius;
            this.temp = temp;
        }

        public readonly int WordCount { get => 5; }
        public readonly void ToWords(Span<float> dst)
        {
            dst[0] = center.x;
            dst[1] = center.y;
            dst[2] = center.z;
            dst[3] = radius;
            dst[4] = temp;
        }
    }

}