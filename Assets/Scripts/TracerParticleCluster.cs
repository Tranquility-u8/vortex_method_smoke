using System;
using UnityEngine;

public class TracerParticleCluster : DrawableParticleCluster<float, TracerParticle> {
    readonly VortexParticleCluster _vortexCluster;
    public VortexParticleCluster VortexCluster { get => _vortexCluster; }
    public TracerParticleCluster(Material material, ComputeShader shader, VortexParticleCluster vortex_cluster, int max_particle_count, int max_emit_count)
        : base(material, shader, max_particle_count, max_emit_count)
    {
        _vortexCluster = vortex_cluster;

        Shader.SetInt("uVortexMaxParticleCount", _vortexCluster.MaxParticleCount);
        Shader.SetBuffer(SimulateKernel, "uVortexParticles", _vortexCluster.ParticleBuffer);
        Shader.SetBuffer(SimulateKernel, "uVortexParticleCount", _vortexCluster.CountBuffer);
    }

    public void Simulate(bool src_flip, bool vortex_src_flip, float delta_time)
    {
        Shader.SetInt("uVortexFlip", vortex_src_flip ? 1 : 0);
        base.Simulate(src_flip, delta_time);
    }
}