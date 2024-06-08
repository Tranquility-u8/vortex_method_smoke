using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke {

    public class Grid {
        private readonly Bounds _bounds;
        private readonly Vector3Int _gridSize;
        private readonly float _cellSize;
        private readonly Texture3D _texture;

        public Bounds Bounds { get => _bounds; }
        public Vector3Int GridSize { get => _gridSize; }
        public float CellSize { get => _cellSize; }
        public Texture3D Texture { get => _texture; }

        public Grid(Bounds bounds, int max_grid_size, TextureFormat texture_format)
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
            // Texture
            _texture = new Texture3D(_gridSize.x, _gridSize.y, _gridSize.z, texture_format, 1);
        }
    }
}