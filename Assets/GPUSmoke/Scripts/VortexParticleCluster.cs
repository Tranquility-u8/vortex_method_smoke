using System;
using UnityEngine;

namespace GPUSmoke
{
    public class VortexParticleCluster : ParticleCluster<float, VortexParticle>
    {
        public VortexParticleCluster(ComputeShader shader, HeatField heat_field, int max_particle_count, int max_emit_count)
            : base(shader, max_particle_count, max_emit_count)
        {
            heat_field.SetShaderUniform(shader, "Heat");
            shader.SetTexture(SimulateKernel, "uHeatTexture", heat_field.Texture);
        }
    }

}