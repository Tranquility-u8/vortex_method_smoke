using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace GPUSmoke
{
    public class ParticleHashGrid<W, T> : Grid where W : unmanaged where T : IStruct<W>, new()
    {
        private readonly ParticleCluster<W, T> _cluster;
        private readonly int _maxSizeBits;

        private readonly ComputeShader _shader;
        private readonly int _sortDispatchKernel, _sortInnerKernel, _sortStepKernel;
        private ComputeBuffer _sortBuffer, _sortCommandBuffer;
        private RenderTexture _texture;
        
        public ParticleHashGrid(ComputeShader shader, ParticleCluster<W, T> cluster, Bounds bounds, int max_grid_size)
            : base(bounds, max_grid_size) {
            _shader = shader;
            _cluster = cluster;
            _maxSizeBits = BitOperation.FindMSB(cluster.MaxParticleCount - 1) + 1;

            _texture = new(GridSize.x, GridSize.y, 0, RenderTextureFormat.RInt, 1)
            {
                volumeDepth = GridSize.z,
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                enableRandomWrite = true,
            };
            
            _sortBuffer = new ComputeBuffer(1 << _maxSizeBits, 2 * sizeof(uint), ComputeBufferType.Structured);
        }
    }

}
