using System;
using UnityEngine;

namespace GPUSmoke
{
    public class VortexParticleCluster : ParticleCluster<float, VortexParticle>
    {
        public VortexParticleCluster(ComputeShader shader, int max_particle_count, int max_emit_count)
            : base(shader, max_particle_count, max_emit_count)
        {
        }
    }

}