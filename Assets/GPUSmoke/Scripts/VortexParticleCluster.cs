using System;
using UnityEngine;

namespace GPUSmoke
{
    public class VortexParticleCluster : ParticleCluster<float, VortexParticle>
    {
        public VortexParticleCluster(
            ComputeShader shader,
            HeatField heat_field,
            VortexMethodConfig vortex_method_config,
            int max_particle_count
            ) : base(shader, max_particle_count)
        {
            vortex_method_config.SetShaderUniform(shader, "VM");
            heat_field.SetShaderUniform(shader, "Heat");
            shader.SetTexture(SimulateKernel, "uHeatTexture", heat_field.Texture);
        }
    }

}