using System.Collections.Generic;
using System.Numerics;
using GPUSmoke.GPUSorting.Runtime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUSmoke
{
    public class ParticleHashGrid<W, T> : Grid where W : unmanaged where T : IStruct<W>, new()
    {
        private readonly ParticleCluster<W, T> _cluster;
        private readonly int _cellCountBitWidth;

        private readonly ComputeShader _shader;
        private readonly int _prepareKernel, _resolveKernel;
        private readonly uint _prepareKernelGroupX, _resolveKernelGroupX;
        private ComputeBuffer _cellIdBuffer, _particleIdBuffer, _rangeBuffer;
        private readonly uint[] _rangeBufferClearData;


        private ComputeBuffer _sorter_tmp0, _sorter_tmp1, _sorter_tmp2, _sorter_tmp3, _sorter_tmp4;
        private DeviceRadixSort _device_radix_sorter;
        private OneSweep _one_sweep_sorter;

        public ParticleCluster<W, T> Cluster { get => _cluster; }

        public ParticleHashGrid(
            ComputeShader device_radix_sorter_shader,
            ComputeShader one_sweep_sorter_shader,
            ComputeShader shader,
            ParticleCluster<W, T> cluster,
            Bounds bounds,
            int max_grid_size
            ) : base(bounds, max_grid_size)
        {
            _shader = shader;
            _cluster = cluster;

            _cellIdBuffer = new ComputeBuffer(cluster.MaxParticleCount, sizeof(uint), ComputeBufferType.Structured);
            _particleIdBuffer = new ComputeBuffer(cluster.MaxParticleCount, sizeof(uint), ComputeBufferType.Structured);
            _rangeBuffer = new ComputeBuffer(CellCount + 1, sizeof(uint) * 2, ComputeBufferType.Structured);
            _rangeBufferClearData = new uint[(CellCount + 1) * 2];
            _cellCountBitWidth = BitOperation.FindMSB(CellCount) + 1;

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan
                )
            {
                _one_sweep_sorter = new(one_sweep_sorter_shader, cluster.MaxParticleCount, ref _sorter_tmp0, ref _sorter_tmp1, ref _sorter_tmp2, ref _sorter_tmp3, ref _sorter_tmp4);
            }
            else
            {
                _device_radix_sorter = new(device_radix_sorter_shader, cluster.MaxParticleCount, ref _sorter_tmp0, ref _sorter_tmp1, ref _sorter_tmp2, ref _sorter_tmp3);
            }

            _prepareKernel = _shader.FindKernel("Prepare");
            _resolveKernel = _shader.FindKernel("Resolve");
            _shader.GetKernelThreadGroupSizes(_prepareKernel, out _prepareKernelGroupX, out _, out _);
            _shader.GetKernelThreadGroupSizes(_resolveKernel, out _resolveKernelGroupX, out _, out _);

            base.SetShaderUniform(_shader);
            cluster.SetShaderBuffer(_shader, _prepareKernel);
            cluster.SetShaderStaticUniform(_shader);

            _shader.SetBuffer(_prepareKernel, "uCellIDs_RW", _cellIdBuffer);
            _shader.SetBuffer(_prepareKernel, "uParticleIDs_RW", _particleIdBuffer);

            _shader.SetBuffer(_resolveKernel, "uCellIDs", _cellIdBuffer);
            _shader.SetBuffer(_resolveKernel, "uRanges_RW", _rangeBuffer);
        }

        public void SetShaderBuffer(ComputeShader shader, int kernel, string prefix = "")
        {
            shader.SetBuffer(kernel, "u" + prefix + "Ranges", _rangeBuffer);
            shader.SetBuffer(kernel, "u" + prefix + "ParticleIDs", _particleIdBuffer);
        }

        public void SetShaderProperty<W_, T_>(ParticleCluster<W_, T_> cluster, string prefix = "") where W_ : unmanaged where T_ : IStruct<W_>, new()
        {
            SetShaderUniform(cluster.Shader, prefix);
            SetShaderBuffer(cluster.Shader, cluster.SimulateKernel, prefix);
        }

        public void Generate(bool flip, int count)
        {
            _rangeBuffer.SetData(_rangeBufferClearData);

            if (count > 0)
            {
                ParticleCluster<W, T>.SetShaderDynamicUniform(_shader, flip, count);
                _shader.Dispatch(_prepareKernel, (int)((count + _prepareKernelGroupX - 1) / _prepareKernelGroupX), 1, 1);
                if (count > 1)
                {
                    if (_device_radix_sorter != null)
                        _device_radix_sorter.SortBitWidth(_cellCountBitWidth, count, _cellIdBuffer, _particleIdBuffer, _sorter_tmp0, _sorter_tmp1, _sorter_tmp2, _sorter_tmp3, typeof(uint), typeof(uint), true);
                    else if (_one_sweep_sorter != null)
                        _one_sweep_sorter.SortBitWidth(_cellCountBitWidth, count, _cellIdBuffer, _particleIdBuffer, _sorter_tmp0, _sorter_tmp1, _sorter_tmp2, _sorter_tmp3, _sorter_tmp4, typeof(uint), typeof(uint), true);
                    else
                        Debug.LogError("No Sorter Available");
                } 
                _shader.Dispatch(_resolveKernel, (int)((count + _resolveKernelGroupX - 1) / _resolveKernelGroupX), 1, 1);
            }
        }

        public void Destroy()
        {
            DestroyUtil.Release(ref _cellIdBuffer);
            DestroyUtil.Release(ref _particleIdBuffer);
            DestroyUtil.Release(ref _rangeBuffer);
            DestroyUtil.Release(ref _sorter_tmp0);
            DestroyUtil.Release(ref _sorter_tmp1);
            DestroyUtil.Release(ref _sorter_tmp2);
            DestroyUtil.Release(ref _sorter_tmp3);
            DestroyUtil.Release(ref _sorter_tmp4);
        }
    }

}
