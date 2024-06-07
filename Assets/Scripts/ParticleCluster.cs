using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class ParticleCluster<W, T> where W : unmanaged where T : IParticleData<W>, new()
{
    private readonly ComputeShader _shader;
    private readonly int _emitKernel, _simulateDispatchKernel, _simulateKernel;
    private readonly uint _emitKernelGroupX;
    private ComputeBuffer _particleBuffer, _emitBuffer, _countBuffer, _simulateCommandBuffer;
    private readonly List<T> _emits;
    private readonly int _maxParticleCount, _maxEmitCount, _particleWordCount;
    private bool _flip = true;

    protected ComputeShader Shader { get => _shader; }
    protected bool Flip { get => _flip; }
    protected ComputeBuffer ParticleBuffer { get => _particleBuffer; }
    protected ComputeBuffer CountBuffer { get => _countBuffer; }
    protected ComputeBuffer SimulateDispatchBuffer { get => _simulateCommandBuffer; }
    protected int MaxParticleCount { get => _maxParticleCount; }
    protected int MaxEmitCount { get => _maxEmitCount; }
    protected int ParticleWordCount { get => _particleWordCount; }

    public List<T> Emits { get => _emits; }

    public ParticleCluster(ComputeShader shader, int max_particle_count, int max_emit_count)
    {
        _shader = shader;
        _maxParticleCount = max_particle_count;
        _maxEmitCount = max_emit_count;
        _particleWordCount = (new T()).WordCount;
        _emits = new List<T>();
        
        _emitKernel = _shader.FindKernel("Emit");
        _shader.GetKernelThreadGroupSizes(_emitKernel, out _emitKernelGroupX, out _, out _);
        _simulateDispatchKernel = _shader.FindKernel("SimulateDispatch");
        _simulateKernel = _shader.FindKernel("Simulate");

        CreateBuffer_();
        SetConstUniform_();
    }

    public virtual void Destroy() {
        DestroyBuffer_();
    }

    public void NewFrame() {
        _flip = !_flip;
        _shader.SetFloat("uFlip", _flip ? 1 : 0);
    }

    public void Emit()
    {
        int emit_count = Math.Min(Emits.Count, _maxEmitCount);
        if (emit_count > 0) {
            // Flatten Data
            W[] emit_data = new W[emit_count * _particleWordCount];
            for (int i = 0, o = 0; i < emit_count; ++i, o += _particleWordCount)
                Emits[i].ToWords(emit_data.AsSpan().Slice(o, _particleWordCount));
            
            // Dispatch
            _shader.SetInt("uEmitCount", emit_count);
            _emitBuffer.SetData(emit_data);
            _shader.Dispatch(_emitKernel, (emit_count + (int)_emitKernelGroupX - 1) / (int)_emitKernelGroupX, 1, 1);
        }
        Emits.Clear();
    }

    public void Simulate()
    {
        _shader.SetFloat("uDeltaTime", Time.deltaTime);
        _shader.Dispatch(_simulateDispatchKernel, 1, 1, 1);
        _shader.DispatchIndirect(_simulateKernel, _simulateCommandBuffer, 0);
    }
    
    private void CreateBuffer_() {
        _particleBuffer = new ComputeBuffer(_maxParticleCount * 2, _particleWordCount * UnsafeUtility.SizeOf<W>(), ComputeBufferType.Structured);
        _emitBuffer = new ComputeBuffer(_maxEmitCount, _particleWordCount * UnsafeUtility.SizeOf<W>(), ComputeBufferType.Structured);
        _countBuffer = new ComputeBuffer(2, sizeof(uint), ComputeBufferType.Structured);
        _simulateCommandBuffer = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.Structured | ComputeBufferType.IndirectArguments);

        uint[] zeros = new uint[2];
        _countBuffer.SetData(zeros);
        
        _shader.SetBuffer(_emitKernel, "uEmits", _emitBuffer);
        _shader.SetBuffer(_emitKernel, "uParticles", _particleBuffer);
        _shader.SetBuffer(_emitKernel, "uParticleCount", _countBuffer);

        _shader.SetBuffer(_simulateDispatchKernel, "uSimulateCommand", _simulateCommandBuffer);
        _shader.SetBuffer(_simulateDispatchKernel, "uParticleCount", _countBuffer);

        _shader.SetBuffer(_simulateKernel, "uParticles", _particleBuffer);
        _shader.SetBuffer(_simulateKernel, "uParticleCount", _countBuffer);
    }

    private void DestroyBuffer_() {
        if (_particleBuffer != null) {
            _particleBuffer.Release();
            _emitBuffer.Release();
            _countBuffer.Release();
            _simulateCommandBuffer.Release();

            _particleBuffer = null;
            _emitBuffer = null;
            _countBuffer = null;
            _simulateCommandBuffer = null;
        }
    }
    private void SetConstUniform_() {
        _shader.SetInt("uMaxParticleCount", _maxParticleCount);
    }
};
