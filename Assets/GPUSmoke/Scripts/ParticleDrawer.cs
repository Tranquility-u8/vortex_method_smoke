using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUSmoke
{
    public class ParticleDrawer<W, T> where W : unmanaged where T : IStruct<W>, new()
    {
        private readonly DrawConfig[] _draws;
        private readonly MaterialPropertyBlock[] _materialPropertyBlocks;
        private readonly Bounds _bounds;

        public ParticleDrawer(List<DrawConfig> draws, ParticleCluster<W, T> cluster, Bounds bounds)
        {
            _bounds = bounds;
            _draws = draws.ToArray();
            _materialPropertyBlocks = new MaterialPropertyBlock[_draws.Count()];
            for (int i = 0; i < _draws.Count(); ++i) {
                _materialPropertyBlocks[i] = new();
                var mpb = _materialPropertyBlocks[i];
                cluster.SetMaterialBuffer(mpb);
                cluster.SetMaterialStaticUniform(mpb);
            }
        }

        public void Draw(bool flip, int count)
        {
            for (int i = 0; i < _draws.Count(); ++i) {
                var d = _draws[i];
                var mpb = _materialPropertyBlocks[i];
                ParticleCluster<W, T>.SetMaterialDynamicUniform(mpb, flip, count);
                Graphics.DrawProcedural(d.Material, _bounds, MeshTopology.Triangles, count * 6, 1, d.Camera, mpb, d.CastShadows, d.ReceiveShadows, d.Layer);
            }
        }
    }

}