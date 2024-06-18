using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX.SDF;

namespace GPUSmoke
{
    class SDF : Grid
    {
        private readonly Texture3D _texture;
        private readonly float _unitDistance;

        public Texture3D Texture { get => _texture; }
        public float UnitDistance { get => _unitDistance; }

        public SDF(Grid grid, RenderTexture texture, float unit_distance) : base(grid)
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
            
            _unitDistance = unit_distance;
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
            _texture.SetPixel(0, 0, 0, new Color(1, 1, 1));
            _texture.Apply();

            _unitDistance = float.PositiveInfinity;
        }

        public override void SetShaderUniform(ComputeShader shader, string prefix = "")
        {
            base.SetShaderUniform(shader, prefix);
            shader.SetFloat("u" + prefix + "UnitDistance", _unitDistance);
        }

        public void SetShaderTexture(ComputeShader shader, int kernel, string prefix = "") {
            shader.SetTexture(kernel, "u" + prefix + "Texture", _texture);
        }

        public void SetShaderProperty<W_, T_>(ParticleCluster<W_, T_> cluster, string prefix = "") where W_ : unmanaged where T_ : IStruct<W_>, new(){
            SetShaderUniform(cluster.Shader, prefix);
            SetShaderTexture(cluster.Shader, cluster.SimulateKernel, prefix);            
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
            float cell_size = (cell_size_3.x + cell_size_3.y + cell_size_3.z) / 3.0f;
            float unit_dist = Math.Max(bounds.size.x, Math.Max(bounds.size.y, bounds.size.z));
            var sdf = new SDF(new Grid(bounds.min, cell_size, grid_size), baker.SdfTexture, unit_dist);
            baker.Dispose();
            return sdf;
        }
    }

}