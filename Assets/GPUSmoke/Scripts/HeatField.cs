using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke
{
    public class HeatField : Grid
    {
        private readonly int _maxEditCount;
        private readonly List<HeatFieldEdit> _edits;

        private readonly ComputeShader _shader;
        private readonly int _editKernel;
        private readonly uint _editKernelGroupX, _editKernelGroupY, _editKernelGroupZ;

        private RenderTexture _texture;
        private ComputeBuffer _editBuffer;

        public List<HeatFieldEdit> Edits { get => _edits; }
        public RenderTexture Texture { get => _texture; }

        public HeatField(ComputeShader shader, Bounds bounds, int max_grid_size, int max_edit_count)
            : base(bounds, max_grid_size)
        {
            // Shader
            _shader = shader;
            _editKernel = shader.FindKernel("Edit");
            _shader.GetKernelThreadGroupSizes(_editKernel, out _editKernelGroupX, out _editKernelGroupY, out _editKernelGroupZ);

            // Edit Buffer
            _maxEditCount = max_edit_count;
            _edits = new();
            _editBuffer = new ComputeBuffer(max_edit_count, StructUtil<float, HeatFieldEdit>.ByteCount);

            // Texture
            _texture = new(GridSize.x, GridSize.y, 0, RenderTextureFormat.RHalf, 1)
            {
                volumeDepth = GridSize.z,
                dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true,
            };


            // Binds
            SetShaderUniform(_shader);
            _shader.SetTexture(_editKernel, "uTexture_RW", _texture);
            _shader.SetBuffer(_editKernel, "uEdits", _editBuffer);
        }
        
        public void Edit() {
            int edit_count = Math.Min(_maxEditCount, _edits.Count);
            if (edit_count == 0) {
                _edits.Clear();
                return;
            }
            // Flatten Data
            int b = _edits.Count - edit_count;
            float[] edit_data = StructUtil<float, HeatFieldEdit>.ToWords(_edits.GetRange(b, edit_count));
            _edits.RemoveRange(b, edit_count);

            // Dispatch
            _shader.SetInt("uEditCount", edit_count);
            _editBuffer.SetData(edit_data);
            _shader.Dispatch(
                _editKernel, 
                (GridSize.x + (int)_editKernelGroupX - 1) / (int)_editKernelGroupX,
                (GridSize.y + (int)_editKernelGroupY - 1) / (int)_editKernelGroupY,
                (GridSize.z + (int)_editKernelGroupZ - 1) / (int)_editKernelGroupZ
            );
        }
        
        public void Destroy() {
            if (_editBuffer != null) {
                _editBuffer.Release();
                _editBuffer = null;
                _texture.Release();
                _texture = null;
            }
        }
    }

}