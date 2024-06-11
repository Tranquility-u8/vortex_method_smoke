using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUSmoke
{
    public class ParticleCluster<W, T> where W : unmanaged where T : IStruct<W>, new()
    {
        private readonly ComputeShader _shader;
        private readonly int _simulateKernel;
        private readonly uint _simulateKernelGroupX;
        private ComputeBuffer _particleBuffer, _pushCountBuffer;
        private readonly List<T> _emits;
        private readonly int _maxParticleCount;

        public ComputeShader Shader { get => _shader; }
        public int SimulateKernel { get => _simulateKernel; }
        public int MaxParticleCount { get => _maxParticleCount; }

        public List<T> Emits { get => _emits; }

        public ParticleCluster(ComputeShader shader, int max_particle_count)
        {
            _shader = shader;
            _maxParticleCount = max_particle_count;
            _emits = new List<T>();

            _simulateKernel = _shader.FindKernel("Simulate");
            _shader.GetKernelThreadGroupSizes(_simulateKernel, out _simulateKernelGroupX, out _, out _);

            _particleBuffer = new ComputeBuffer(_maxParticleCount * 2, StructUtil<W, T>.ByteCount, ComputeBufferType.Structured);
            _pushCountBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);
            uint[] zeros = new uint[1];
            _pushCountBuffer.SetData(zeros);

            _shader.SetBuffer(_simulateKernel, "uParticles", _particleBuffer);
            _shader.SetBuffer(_simulateKernel, "uParticles_RW", _particleBuffer);
            _shader.SetBuffer(_simulateKernel, "uPushCount_RW", _pushCountBuffer);
            SetShaderStaticUniform(_shader);
        }

        public virtual void Destroy()
        {
            DestroyUtil.Release(ref _particleBuffer);
            DestroyUtil.Release(ref _pushCountBuffer);
        }
        
        public void SetShaderBuffer(ComputeShader shader, int kernel, string prefix = "") {
            shader.SetBuffer(kernel, "u" + prefix + "Particles", _particleBuffer);
        }
        public void SetMaterialBuffer(Material shader, string prefix = "") {
            shader.SetBuffer("u" + prefix + "Particles", _particleBuffer);
        }
        public void SetShaderStaticUniform(ComputeShader shader, string prefix = "") {
            shader.SetInt("u" + prefix + "MaxCount", _maxParticleCount);
        }
        public void SetMaterialStaticUniform(Material shader, string prefix = "") {
            shader.SetInt("u" + prefix + "MaxCount", _maxParticleCount);
        }
        public static void SetShaderDynamicUniform(ComputeShader shader, bool flip, int count, string prefix = "") {
            shader.SetInt("u" + prefix + "Flip", flip ? 1 : 0);
            shader.SetInt("u" + prefix + "Count", count);
        }
        public static void SetMaterialDynamicUniform(Material shader, bool flip, int count, string prefix = "") {
            shader.SetInt("u" + prefix + "Count", count);
            shader.SetInt("u" + prefix + "Flip", flip ? 1 : 0);
        }

        public void Simulate(bool src_flip, float delta_time, Action<bool, int> on_simulate)
        {
            int src_count;
            {
                // Fetch SRC Count
                int[] count_data = new int[1];
                _pushCountBuffer.GetData(count_data);
                src_count = Math.Min(count_data[0], _maxParticleCount);

                // Set DST Count & Emit
                int dst_count = Math.Min(Emits.Count, _maxParticleCount);
                W[] dst_data = StructUtil<W, T>.ToWords(Emits.GetRange(0, dst_count));
                count_data[0] = dst_count;
                Emits.Clear();
                _pushCountBuffer.SetData(count_data);
                _particleBuffer.SetData(
                    dst_data, 
                    0, 
                    src_flip ? 0 : _maxParticleCount * StructUtil<W, T>.WordCount, 
                    dst_count * StructUtil<W, T>.WordCount
                );
            }

            on_simulate(src_flip, src_count);

            if (src_count > 0)
            {
                SetShaderDynamicUniform(_shader, src_flip, src_count);
                _shader.SetFloat("uDeltaTime", delta_time);
                _shader.Dispatch(_simulateKernel, (int)((src_count + _simulateKernelGroupX - 1) / _simulateKernelGroupX), 1, 1);
            }
        }
    }

}