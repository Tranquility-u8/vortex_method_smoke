using System;
using UnityEngine;

namespace GPUSmoke
{
    public struct HeatFieldEdit : IStruct<float>
    {
        public Vector3 center;
        public float heat, stddev;

        public HeatFieldEdit(Vector3 center, float heat, float stddev)
        {
            this.center = center;
            this.heat = heat;
            this.stddev = stddev;
        }

        public readonly int WordCount { get => 5; }
        public readonly void ToWords(Span<float> dst)
        {
            dst[0] = center.x;
            dst[1] = center.y;
            dst[2] = center.z;
            dst[3] = heat;
            dst[4] = stddev;
        }
    }

}