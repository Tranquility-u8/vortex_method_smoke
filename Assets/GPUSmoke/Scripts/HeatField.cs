using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace GPUSmoke
{
    public class HeatField : Grid
    {
        private readonly int _maxEntryCount;
        private int _allocEntryID = 0;
        private bool _entryChanged = false;
        private readonly Dictionary<HeatFieldEntryID, HeatFieldEntry> _entries;

        private readonly ComputeShader _shader;
        private readonly int _updateKernel;
        private readonly uint _updateKernelGroupX, _updateKernelGroupY, _updateKernelGroupZ;

        private RenderTexture _texture;
        private ComputeBuffer _entryBuffer;


        public HeatField(ComputeShader shader, Bounds bounds, int max_grid_size, int max_entry_count)
            : base(bounds, max_grid_size)
        {
            // Shader
            _shader = shader;
            _updateKernel = shader.FindKernel("Update");
            _shader.GetKernelThreadGroupSizes(_updateKernel, out _updateKernelGroupX, out _updateKernelGroupY, out _updateKernelGroupZ);

            // Entry Buffer
            _maxEntryCount = max_entry_count;
            _entries = new();
            _entryBuffer = new ComputeBuffer(max_entry_count, StructUtil<float, HeatFieldEntry>.ByteCount);

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
            _shader.SetTexture(_updateKernel, "uTexture_RW", _texture);
            _shader.SetBuffer(_updateKernel, "uEntries", _entryBuffer);
        }
        
        public void SetShaderTexture(ComputeShader shader, int kernel, string prefix = "") {
            shader.SetTexture(kernel, "u" + prefix + "Texture", _texture);
        }

        public void SetShaderProperty<W_, T_>(ParticleCluster<W_, T_> cluster, string prefix = "") where W_ : unmanaged where T_ : IStruct<W_>, new(){
            SetShaderUniform(cluster.Shader, prefix);
            SetShaderTexture(cluster.Shader, cluster.SimulateKernel, prefix);            
        }

        public HeatFieldEntryID AddEntry(HeatFieldEntry entry)
        {
            var entry_id = (HeatFieldEntryID)_allocEntryID++;
            _entries[entry_id] = entry;
            _entryChanged = true;
            return entry_id;
        }
        public void SetEntry(HeatFieldEntryID entry_id, HeatFieldEntry entry)
        {
            var prev_entry = _entries[entry_id];
            if (!entry.Equals(prev_entry))
                _entryChanged = true;
            _entries[entry_id] = entry;
        }
        public void RemoveEntry(HeatFieldEntryID entry_id)
        {
            _entries.Remove(entry_id);
            _entryChanged = true;
        }

        public void Update()
        {
            if (_entryChanged == false)
                return;
            _entryChanged = false;

            // Transfer
            int entry_count = Math.Min(_entries.Count, _maxEntryCount);
            float[] entry_data = StructUtil<float, HeatFieldEntry>.ToWords(_entries.Values.Take(entry_count), entry_count);
            _entryBuffer.SetData(entry_data);

            // Dispatch
            _shader.SetInt("uEntryCount", entry_count);
            _shader.Dispatch(
                _updateKernel,
                (GridSize.x + (int)_updateKernelGroupX - 1) / (int)_updateKernelGroupX,
                (GridSize.y + (int)_updateKernelGroupY - 1) / (int)_updateKernelGroupY,
                (GridSize.z + (int)_updateKernelGroupZ - 1) / (int)_updateKernelGroupZ
            );
        }

        public void Destroy()
        {
            DestroyUtil.Release(ref _entryBuffer);
            DestroyUtil.Release(ref _texture);
        }
    }

}