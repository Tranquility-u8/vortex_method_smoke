using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.VFX;
using UnityEngine.VFX.SDF;

namespace GPUSmoke
{
    class SDF : Grid
    {
        private Texture3D _texture;

        public Texture3D Texture { get => _texture; }

        public SDF(Grid grid, Texture3D texture) : base(grid)
        {
            _texture = texture;
        }

        public SDF(Grid grid, RenderTexture texture) : base(grid)
        {
            _texture = new(GridSize.x, GridSize.y, GridSize.z, 
                texture.graphicsFormat,
                UnityEngine.Experimental.Rendering.TextureCreationFlags.None
            );
            Graphics.CopyTexture(texture, _texture);
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.filterMode = FilterMode.Bilinear;
        }

        public SDF() : base(new Bounds(Vector3.zero, Vector3.one), 1)
        {
            _texture = new(GridSize.x, GridSize.y, GridSize.z, 
                UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat, 
                UnityEngine.Experimental.Rendering.TextureCreationFlags.None
            );
            Assert.AreEqual(GridSize, Vector3Int.one);
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.filterMode = FilterMode.Point;
            _texture.SetPixel(0, 0, 0, new Color(float.PositiveInfinity, 0, 0));
            _texture.Apply();
        }

        public static SDF Bake(Bounds bounds, int max_grid_size, Mesh mesh)
        {
            if (mesh.vertexCount == 0)
                return new();

            Grid grid = new(bounds, max_grid_size);
            bounds = grid.Bounds;
            MeshToSDFBaker baker = new(bounds.size, bounds.center, max_grid_size, mesh);
            baker.BakeSDF();
            Assert.AreEqual(baker.GetGridSize(), grid.GridSize);
            var sdf = new SDF(grid, baker.SdfTexture);
            baker.Dispose();
            return sdf;
        }
    }

}