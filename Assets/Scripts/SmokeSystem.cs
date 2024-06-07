using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSystem : MonoBehaviour
{
    public ComputeShader VortexComputeShader, TracerComputeShader;
    public int MaxVortexParticleCount, MaxVortexEmitCount;
    public int MaxTracerParticleCount, MaxTracerEmitCount;
    public Bounds ParticleBounds;
    public Material ParticleMaterial;

    public List<VortexParticle> VortexEmits { get => _vortexCluster.Emits; }
    public List<TracerParticle> TracerEmits { get => _tracerCluster.Emits; }


    private VortexParticleCluster _vortexCluster;
    private TracerParticleCluster _tracerCluster;
    private bool _flip = true;

    void Awake()
    {
        _vortexCluster = new(VortexComputeShader, MaxVortexParticleCount, MaxVortexEmitCount);
        _tracerCluster = new(ParticleMaterial, TracerComputeShader, _vortexCluster, MaxTracerParticleCount, MaxTracerEmitCount);
    }

    void OnDisable()
    {
        _vortexCluster.Destroy();
        _vortexCluster = null;
        _tracerCluster.Destroy();
        _tracerCluster = null;
    }

    void Update()
    {
        float x = UnityEngine.Random.Range(-0.2f, 0.2f);
        float y = UnityEngine.Random.Range(-0.2f, 0.2f);
        float z = UnityEngine.Random.Range(-0.2f, 0.2f);
        _tracerCluster.Emits.Add(new TracerParticle(new Vector3(x, y, z), 1.0f));

        _vortexCluster.Emit(_flip);
        _tracerCluster.Emit(_flip);

        _vortexCluster.Simulate(_flip, Time.deltaTime);
        _tracerCluster.Simulate(_flip, !_flip, Time.deltaTime);
        _tracerCluster.Draw(!_flip, ParticleBounds);
        
        _flip = !_flip;
    }
}
