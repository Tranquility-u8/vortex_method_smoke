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

        private readonly ComputeBuffer _editBuffer;

        public List<HeatFieldEdit> Edits { get => _edits; }

        public HeatField(ComputeShader shader, Bounds bounds, int max_grid_size, int max_edit_count)
            : base(bounds, max_grid_size, TextureFormat.RHalf)
        {
            // Shader
            _shader = shader;
            _editKernel = shader.FindKernel("Edit");

            // Edit Buffer
            _maxEditCount = max_edit_count;
            _edits = new();
            _editBuffer = new ComputeBuffer(max_edit_count, StructUtil<float, HeatFieldEdit>.ByteCount);
        }
    }

}