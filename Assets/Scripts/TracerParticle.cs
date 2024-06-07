using System;
using UnityEngine;

public struct TracerParticle : IParticle<float>
{
    public Vector3 pos;
    public float life;

    public TracerParticle(Vector3 pos, float life)
    {
        this.pos = pos;
        this.life = life;
    }

    public readonly int WordCount { get => 4; }
    public readonly void ToWords(Span<float> dst)
    {
        dst[0] = pos.x;
        dst[1] = pos.y;
        dst[2] = pos.z;
        dst[3] = life;
    }
}