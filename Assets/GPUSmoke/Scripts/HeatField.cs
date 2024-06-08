using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke
{
    public class HeatField
    {
        private readonly Bounds _bounds;
        private readonly Vector3Int _gridSize;
        private readonly float _cellSize;
        private readonly int _maxEditCount;

        private readonly ComputeShader _shader;
        private readonly int _editKernel;

        private Texture3D _texture;
        
        private ComputeBuffer _editBuffer;
        
        public HeatField(ComputeShader shader, Bounds bounds, int max_grid_size, int max_edit_count) {
            // Properties
            _bounds = bounds;
            _cellSize = Math.Max(Math.Max(bounds.size.x, bounds.size.y), bounds.size.z) / max_grid_size;
            var grid_size_f = bounds.size / _cellSize;
            _gridSize = new Vector3Int(
                Convert.ToInt32(Math.Ceiling(grid_size_f.x)), 
                Convert.ToInt32(Math.Ceiling(grid_size_f.y)), 
                Convert.ToInt32(Math.Ceiling(grid_size_f.z))
            );
            _maxEditCount = max_edit_count;
            
            // Shader
            _shader = shader;
            _editKernel = shader.FindKernel("Edit");
            
            // Texture
            _texture = new Texture3D(_gridSize.x, _gridSize.y, _gridSize.z, TextureFormat.RHalf, 1);
            
            // Buffer
            _editBuffer = new ComputeBuffer(max_edit_count, StructUtil<float, HeatFieldEdit>.ByteCount);
        }
    }

}