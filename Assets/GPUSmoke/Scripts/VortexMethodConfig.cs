using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke
{
    public struct VortexMethodConfig
    {
        public float epsilon, heat_buoyancy_factor;

        public readonly void SetShaderUniform(ComputeShader shader, string prefix = "")
        {
            shader.SetFloat("u" + prefix + "InvEpsilon2", 1.0f / (epsilon * epsilon));
            shader.SetFloat("u" + prefix + "HeatBuoyancyFactor", heat_buoyancy_factor);
        }
    }

}