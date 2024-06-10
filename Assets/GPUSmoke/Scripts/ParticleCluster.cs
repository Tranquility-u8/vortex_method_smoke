using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace GPUSmoke
{
    public class ParticleCluster<W, T> where W : unmanaged where T : IStruct<W>, new()
    {
        private readonly ComputeShader _shader;
        private readonly int _emitKernel, _simulateDispatchKernel, _simulateKernel;
        private readonly uint _emitKernelGroupX;
        private ComputeBuffer _particleBuffer, _emitBuffer, _countBuffer, _simulateCommandBuffer;
        private readonly List<T> _emits;
        private readonly int _maxParticleCount, _maxEmitCount;

        public ComputeShader Shader { get => _shader; }
        public int EmitKernel { get => _emitKernel; }
        public int SimulateDispatchKernel { get => _simulateDispatchKernel; }
        public int SimulateKernel { get => _simulateKernel; }
        public ComputeBuffer ParticleBuffer { get => _particleBuffer; }
        public ComputeBuffer CountBuffer { get => _countBuffer; }
        public ComputeBuffer SimulateDispatchBuffer { get => _simulateCommandBuffer; }
        public int MaxParticleCount { get => _maxParticleCount; }
        public int MaxEmitCount { get => _maxEmitCount; }

        public List<T> Emits { get => _emits; }

        public ParticleCluster(ComputeShader shader, int max_particle_count, int max_emit_count)
        {
            _shader = shader;
            _maxParticleCount = max_particle_count;
            _maxEmitCount = max_emit_count;
            _emits = new List<T>();

            _emitKernel = _shader.FindKernel("Emit");
            _shader.GetKernelThreadGroupSizes(_emitKernel, out _emitKernelGroupX, out _, out _);
            _simulateDispatchKernel = _shader.FindKernel("SimulateDispatch");
            _simulateKernel = _shader.FindKernel("Simulate");

            CreateBuffer_();
            SetConstUniform_();
        }

        public virtual void Destroy()
        {
            DestroyBuffer_();
        }

        public void Emit(bool src_flip)
        {
            int emit_count = Math.Min(Emits.Count, _maxEmitCount);
            if (emit_count == 0)
            {
                Emits.Clear();
                return;
            }

            // Flatten Data
            W[] emit_data = StructUtil<W, T>.ToWords(Emits.GetRange(0, emit_count));
            Emits.Clear();

            // Dispatch
            _shader.SetInt("uFlip", src_flip ? 1 : 0);
            _shader.SetInt("uEmitCount", emit_count);
            _emitBuffer.SetData(emit_data);
            _shader.Dispatch(_emitKernel, (emit_count + (int)_emitKernelGroupX - 1) / (int)_emitKernelGroupX, 1, 1);
        }

        public void Simulate(bool src_flip, float delta_time)
        {
            _shader.SetInt("uFlip", src_flip ? 1 : 0);
            _shader.SetFloat("uDeltaTime", delta_time);
            _shader.Dispatch(_simulateDispatchKernel, 1, 1, 1);
            _shader.DispatchIndirect(_simulateKernel, _simulateCommandBuffer, 0);
        }

        private void CreateBuffer_()
        {
            _particleBuffer = new ComputeBuffer(_maxParticleCount * 2, StructUtil<W, T>.ByteCount, ComputeBufferType.Structured);
            _emitBuffer = new ComputeBuffer(_maxEmitCount, StructUtil<W, T>.ByteCount, ComputeBufferType.Structured);
            _countBuffer = new ComputeBuffer(2, sizeof(uint), ComputeBufferType.Structured);
            _simulateCommandBuffer = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.Structured | ComputeBufferType.IndirectArguments);

            uint[] zeros = new uint[2];
            _countBuffer.SetData(zeros);

            _shader.SetBuffer(_emitKernel, "uEmits", _emitBuffer);
            _shader.SetBuffer(_emitKernel, "uParticles", _particleBuffer);
            _shader.SetBuffer(_emitKernel, "uParticles_RW", _particleBuffer);
            _shader.SetBuffer(_emitKernel, "uParticleCount", _countBuffer);
            _shader.SetBuffer(_emitKernel, "uParticleCount_RW", _countBuffer);

            _shader.SetBuffer(_simulateDispatchKernel, "uSimulateCommand_RW", _simulateCommandBuffer);
            _shader.SetBuffer(_simulateDispatchKernel, "uParticleCount", _countBuffer);
            _shader.SetBuffer(_simulateDispatchKernel, "uParticleCount_RW", _countBuffer);

            _shader.SetBuffer(_simulateKernel, "uParticles", _particleBuffer);
            _shader.SetBuffer(_simulateKernel, "uParticles_RW", _particleBuffer);
            _shader.SetBuffer(_simulateKernel, "uParticleCount", _countBuffer);
            _shader.SetBuffer(_simulateKernel, "uParticleCount_RW", _countBuffer);
        }

        private void DestroyBuffer_()
        {
            if (_particleBuffer != null)
            {
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
        private void SetConstUniform_()
        {
            _shader.SetInt("uMaxParticleCount", _maxParticleCount);
        }
    }

}