using System;
using UnityEngine;

namespace GPUSmoke
{
    public class VortexParticleCluster : ParticleCluster<float, VortexParticle>
    {
        public VortexParticleCluster(
            ComputeShader shader,
            VortexMethodConfig vortex_method_config,
            int max_particle_count
            ) : base(shader, max_particle_count)
        {
            vortex_method_config.SetShaderUniform(shader, "VM");
        }
    }

}