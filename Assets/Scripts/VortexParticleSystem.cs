using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VortexParticleSystem : MonoBehaviour
{
    public struct VortexParticle : IParticleData<float>
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
    public struct TracerParticle : IParticleData<float>
    {
        public Vector3 pos;
        public float life;

        public readonly int WordCount { get => 4; }
        public readonly void ToWords(Span<float> dst)
        {
            dst[0] = pos.x;
            dst[1] = pos.y;
            dst[2] = pos.z;
            dst[3] = life;
        }
    }
    public ComputeShader VortexComputeShader, TracerComputeShader;
    public int MaxVortexParticleCount, MaxVortexEmitCount;
    public int MaxTracerParticleCount, MaxTracerEmitCount;
    public Bounds ParticleBounds;
    public Material ParticleMaterial;


    private DrawableParticleCluster<float, VortexParticle> _vortexCluster;

    private DrawableParticleCluster<float, TracerParticle> _tracerCluster;

    void Start()
    {
        _vortexCluster = new DrawableParticleCluster<float, VortexParticle>(ParticleMaterial, VortexComputeShader, MaxVortexParticleCount, MaxVortexEmitCount);
    }

    void OnDisable()
    {
        _vortexCluster.Destroy();
        _vortexCluster = null;
    }

    void Update()
    {
        float x = UnityEngine.Random.Range(-0.2f, 0.2f);
        float y = UnityEngine.Random.Range(-0.2f, 0.2f);
        float z = UnityEngine.Random.Range(-0.2f, 0.2f);
        _vortexCluster.Emits.Add(new VortexParticle(new Vector3(x, y, z), Vector3.zero, 1.0f));
        _vortexCluster.NewFrame();
        _vortexCluster.Emit();
        _vortexCluster.Simulate();
        _vortexCluster.Draw(ParticleBounds);
    }
}
