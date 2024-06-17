using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.SDF;

namespace GPUSmoke
{
    class SDF : Grid
    {
        private MeshToSDFBaker _baker;

        public SDF(Bounds bounds, int max_grid_size, List<MeshFilter> meshes)
            : base(bounds, max_grid_size)
        {
            // _baker = new MeshToSDFBaker()
        }
    }

}