using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

namespace GPUSmoke
{
    public class ParticleHashGrid<W, T> : Grid where W : unmanaged where T : IStruct<W>, new()
    {
        private readonly ParticleCluster<W, T> _cluster;
        private readonly int _maxSizeBits;

        private readonly ComputeShader _shader;
        private readonly int _sortDispatchKernel, _sort512Kernel, _sortInner512Kernel, _sortStepKernel;
        private ComputeBuffer _sortEntryBuffer, _sortCommandBuffer, _gridOffsetBuffer;
        
        public ParticleCluster<W, T> Cluster { get => _cluster; }
        
        public ParticleHashGrid(ComputeShader shader, ParticleCluster<W, T> cluster, Bounds bounds, int max_grid_size)
            : base(bounds, max_grid_size) {
            _shader = shader;
            _cluster = cluster;
            _maxSizeBits = BitOperation.FindMSB(cluster.MaxParticleCount - 1) + 1;

            _sortEntryBuffer = new ComputeBuffer(1 << _maxSizeBits, 2 * sizeof(uint), ComputeBufferType.Structured);
            _sortCommandBuffer = new ComputeBuffer(_maxSizeBits + 1, 3 * sizeof(uint), ComputeBufferType.Structured | ComputeBufferType.IndirectArguments);
            _gridOffsetBuffer = new ComputeBuffer(CellCount + 1, sizeof(uint), ComputeBufferType.Structured);
            
            _sortDispatchKernel = _shader.FindKernel("SortDispatch");
            _sort512Kernel = _shader.FindKernel("Sort512");
            _sortInner512Kernel = _shader.FindKernel("SortInner512");
            _sortStepKernel = _shader.FindKernel("SortStep");
        }
        
        public void Generate(bool src_flip) {
        }
        
        public void Destroy() {
            if (_gridOffsetBuffer != null) {
                _gridOffsetBuffer.Release();
                _gridOffsetBuffer = null;
                _sortEntryBuffer.Release();
                _sortEntryBuffer = null;
                _sortCommandBuffer.Release();
                _sortCommandBuffer = null;
            }
        }
    }

}
