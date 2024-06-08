using System;
using UnityEngine;

namespace GPUSmoke
{
    public struct VortexParticle : IStruct<float>
    {
        public Vector3 pos, vor;
        public float life;

        public VortexParticle(Vector3 pos, Vector3 vor, float life)
        {
            this.pos = pos;
            this.vor = vor;
            this.life = life;
        }

        public readonly int WordCount { get => 7; }
        public readonly void ToWords(Span<float> dst)
        {
            dst[0] = pos.x;
            dst[1] = pos.y;
            dst[2] = pos.z;
            dst[3] = vor.x;
            dst[4] = vor.y;
            dst[5] = vor.z;
            dst[6] = life;
        }
    }

}