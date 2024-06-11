using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke
{
    public class ParticleDrawer<W, T> where W : unmanaged where T : IStruct<W>, new()
    {
        private readonly List<Material> _materials;
        private readonly Bounds _bounds;

        public List<Material> Materials { get => _materials; }

        public ParticleDrawer(List<Material> materials, ParticleCluster<W, T> cluster, Bounds bounds)
        {
            _materials = materials;
            _bounds = bounds;
            foreach (Material m in _materials) {
                cluster.SetMaterialBuffer(m);
                cluster.SetMaterialStaticUniform(m);
            }
        }

        public void Draw(bool flip, int count)
        {
            foreach (Material m in _materials) {
                ParticleCluster<W, T>.SetMaterialDynamicUniform(m, flip, count);
                Graphics.DrawProcedural(m, _bounds, MeshTopology.Triangles, count * 6);
            }
        }
    }

}