using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX.SDF;

namespace GPUSmoke
{
    class SDF : Grid
    {
        private readonly Texture3D _texture;

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
            )
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Graphics.CopyTexture(texture, _texture);
        }

        public SDF() : base(Vector3.zero, 1.0f, Vector3Int.one)
        {
            _texture = new(GridSize.x, GridSize.y, GridSize.z,
                UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat,
                UnityEngine.Experimental.Rendering.TextureCreationFlags.None
            )
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            _texture.SetPixel(0, 0, 0, new Color(float.PositiveInfinity, 0, 0));
            _texture.Apply();
        }

        public static SDF Bake(Bounds bounds, int max_grid_size, Mesh mesh)
        {
            if (mesh.vertexCount == 0)
                return new();

            MeshToSDFBaker baker = new(bounds.size, bounds.center, max_grid_size, mesh);
            baker.BakeSDF();
            
            bounds = new Bounds(bounds.center, baker.GetActualBoxSize());
            Vector3Int grid_size = baker.GetGridSize();
            var cell_size_3 = new Vector3(
                bounds.size.x / grid_size.x,
                bounds.size.y / grid_size.y,
                bounds.size.z / grid_size.z
            );
            // Debug.Log(cell_size_3);
            float cell_size = (cell_size_3.x + cell_size_3.y + cell_size_3.z) / 3.0f;
            var sdf = new SDF(new Grid(bounds.min, cell_size, grid_size), baker.SdfTexture);
            baker.Dispose();
            return sdf;
        }
    }

}