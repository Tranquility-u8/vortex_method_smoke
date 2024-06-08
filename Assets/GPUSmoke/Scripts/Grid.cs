using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace GPUSmoke
{

    public class Grid
    {
        private readonly Bounds _bounds;
        private readonly Vector3Int _gridSize;
        private readonly float _cellSize;

        public Bounds Bounds { get => _bounds; }
        public Vector3Int GridSize { get => _gridSize; }
        public float CellSize { get => _cellSize; }

        public Grid(Bounds bounds, int max_grid_size)
        {
            // Properties
            _bounds = bounds;
            _cellSize = Math.Max(Math.Max(bounds.size.x, bounds.size.y), bounds.size.z) / max_grid_size;
            var grid_size_f = bounds.size / _cellSize;
            _gridSize = new Vector3Int(
                Convert.ToInt32(Math.Ceiling(grid_size_f.x)),
                Convert.ToInt32(Math.Ceiling(grid_size_f.y)),
                Convert.ToInt32(Math.Ceiling(grid_size_f.z))
            );
        }

        public void SetShaderUniform(ComputeShader shader, string prefix = "")
        {
            shader.SetFloat("u" + prefix + "CellSize", _cellSize);
            float[] bound_min = new float[3] {_bounds.min.x, _bounds.min.y, _bounds.min.z};
            shader.SetFloats("u" + prefix + "BoundMin", bound_min);
            int[] grid_size = new int[3] {_gridSize.x, _gridSize.y, _gridSize.z};
            shader.SetInts("u" + prefix + "GridSize", grid_size);
        }
    }
}