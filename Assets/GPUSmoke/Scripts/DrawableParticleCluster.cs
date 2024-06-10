using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke
{
    public class DrawableParticleCluster<W, T> : ParticleCluster<W, T> where W : unmanaged where T : IStruct<W>, new()
    {
        private ComputeBuffer _drawCommandBuffer;
        private readonly int _drawDispatchKernel;
        private readonly Material _material;

        public ComputeBuffer DrawDispatchBuffer { get => _drawCommandBuffer; }
        public int DrawDispatchKernel { get => _drawDispatchKernel; }
        public Material Material { get => _material; }

        public DrawableParticleCluster(Material material, ComputeShader shader, int max_particle_count, int max_emit_count)
            : base(shader, max_particle_count, max_emit_count)
        {
            _drawCommandBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.Structured | ComputeBufferType.IndirectArguments);
            _drawDispatchKernel = Shader.FindKernel("DrawDispatch");
            _material = material;
            Shader.SetBuffer(_drawDispatchKernel, "uDrawCommand_RW", _drawCommandBuffer);
            Shader.SetBuffer(_drawDispatchKernel, "uParticleCount", CountBuffer);
            Shader.SetBuffer(_drawDispatchKernel, "uParticleCount_RW", CountBuffer);

            _material.SetBuffer("uParticles", ParticleBuffer);
            _material.SetInt("uMaxParticleCount", MaxParticleCount);
        }

        public override void Destroy()
        {
            base.Destroy();
            if (_drawCommandBuffer != null)
            {
                _drawCommandBuffer.Release();
                _drawCommandBuffer = null;
            }
        }

        public void Draw(bool src_flip, Bounds bounds)
        {
            Shader.SetInt("uFlip", src_flip ? 1 : 0);
            _material.SetInt("uFlip", src_flip ? 1 : 0);

            Shader.Dispatch(_drawDispatchKernel, 1, 1, 1);
            Graphics.DrawProceduralIndirect(_material, bounds, MeshTopology.Triangles, _drawCommandBuffer);
        }
    }

}