using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawableParticleCluster<W, T> : ParticleCluster<W, T> where W : unmanaged where T : IParticleData<W>, new()
{
    private ComputeBuffer _drawCommandBuffer;
    private readonly int _drawDispatchKernel;
    private readonly Material _material;
    public DrawableParticleCluster(Material material, ComputeShader shader, int max_particle_count, int max_emit_count)
        : base(shader, max_particle_count, max_emit_count)
    {
        _drawCommandBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.Structured | ComputeBufferType.IndirectArguments);
        _drawDispatchKernel = Shader.FindKernel("DrawDispatch");
        _material = material;
        Shader.SetBuffer(_drawDispatchKernel, "uDrawCommand", _drawCommandBuffer);
        Shader.SetBuffer(_drawDispatchKernel, "uParticleCount", CountBuffer);

        _material.SetBuffer("uParticles", ParticleBuffer);
        _material.SetInt("uMaxParticleCount", MaxParticleCount);
    }
    
    public override void Destroy() {
        base.Destroy();
        if (_drawCommandBuffer != null) {
            _drawCommandBuffer.Release();
            _drawCommandBuffer = null;
        }
    }
    
    public void Draw(Bounds bounds) {
        Shader.Dispatch(_drawDispatchKernel, 1, 1, 1);
        _material.SetInt("uFlip", Flip ? 1 : 0);
        Graphics.DrawProceduralIndirect(_material, bounds, MeshTopology.Points, _drawCommandBuffer);
    }
}