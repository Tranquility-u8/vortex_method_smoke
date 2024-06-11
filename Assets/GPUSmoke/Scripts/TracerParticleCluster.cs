using System;
using UnityEngine;

namespace GPUSmoke
{
    public class TracerParticleCluster : ParticleCluster<float, TracerParticle>
    {
        readonly VortexParticleCluster _vortexCluster;
        public VortexParticleCluster VortexCluster { get => _vortexCluster; }
        public TracerParticleCluster(
            ComputeShader shader, 
            HeatField heat_field,
            VortexMethodConfig vortex_method_config, 
            VortexParticleCluster vortex_cluster, 
            int max_particle_count
            ) : base(shader, max_particle_count)
        {
            _vortexCluster = vortex_cluster;

            vortex_method_config.SetShaderUniform(shader, "VM");
            _vortexCluster.SetShaderBuffer(Shader, SimulateKernel, "Vortex");
            _vortexCluster.SetShaderStaticUniform(Shader, "Vortex");

            heat_field.SetShaderUniform(shader, "Heat");
            shader.SetTexture(SimulateKernel, "uHeatTexture", heat_field.Texture);
        }

        public void Simulate(bool src_flip, bool vortex_flip, int vortex_count, float delta_time, Action<bool, int> on_simulate) {
            VortexParticleCluster.SetShaderDynamicUniform(Shader, vortex_flip, vortex_count, "Vortex");
            base.Simulate(src_flip, delta_time, on_simulate);
        }
    }

}