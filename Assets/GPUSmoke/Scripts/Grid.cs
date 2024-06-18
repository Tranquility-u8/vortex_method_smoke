using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace GPUSmoke
{

    public class Grid
    {
        private readonly Vector3 _boundMin;
        private readonly Vector3Int _gridSize;
        private readonly float _cellSize;

        public Vector3 BoundMin { get => _boundMin; }
        public Bounds Bounds { get => new(
            new Vector3(
                _boundMin.x + _gridSize.x * 0.5f * _cellSize,
                _boundMin.y + _gridSize.y * 0.5f * _cellSize,
                _boundMin.z + _gridSize.z * 0.5f * _cellSize
            ),
            new Vector3(
                _gridSize.x * _cellSize,
                _gridSize.y * _cellSize,
                _gridSize.z * _cellSize
            )
        ); }
        public Vector3Int GridSize { get => _gridSize; }
        public float CellSize { get => _cellSize; }
        public int CellCount { get => _gridSize.x * _gridSize.y * _gridSize.z; }

        public Grid(Bounds bounds, int max_grid_size)
        {
            // Properties
            _boundMin = bounds.min;
            _cellSize = Math.Max(Math.Max(bounds.size.x, bounds.size.y), bounds.size.z) / max_grid_size;
            var grid_size_f = bounds.size / _cellSize;
            _gridSize = new Vector3Int(
                Convert.ToInt32(Math.Ceiling(grid_size_f.x)),
                Convert.ToInt32(Math.Ceiling(grid_size_f.y)),
                Convert.ToInt32(Math.Ceiling(grid_size_f.z))
            );
        }

        public Grid(Grid grid)
        {
            _boundMin = grid._boundMin;
            _cellSize = grid._cellSize;
            _gridSize = grid._gridSize;
        }
        
        public Grid(Vector3 bound_min, float cell_size, Vector3Int grid_size) {
            _boundMin = bound_min;
            _cellSize = cell_size;
            _gridSize = grid_size;
        }

        public virtual void SetShaderUniform(ComputeShader shader, string prefix = "")
        {
            shader.SetFloat("u" + prefix + "CellSize", _cellSize);
            float[] bound_min = new float[3] {_boundMin.x, _boundMin.y, _boundMin.z};
            shader.SetFloats("u" + prefix + "BoundMin", bound_min);
            int[] grid_size = new int[3] {_gridSize.x, _gridSize.y, _gridSize.z};
            shader.SetInts("u" + prefix + "GridSize", grid_size);
        }
    }
}