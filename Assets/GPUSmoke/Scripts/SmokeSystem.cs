using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke
{
    public class SmokeSystem : MonoBehaviour
    {
        public Bounds Bounds;

        public ComputeShader VortexComputeShader, TracerComputeShader;
        public int MaxVortexParticleCount, MaxVortexEmitCount;
        public int MaxTracerParticleCount, MaxTracerEmitCount;
        public Material ParticleMaterial;

        public ComputeShader HeatFieldShader;
        public int HeatFieldMaxGridSize, HeatFieldMaxEditCount;

        private VortexParticleCluster _vortexCluster;
        private TracerParticleCluster _tracerCluster;
        private HeatField _heatField;
        private bool _flip = true;

        public List<VortexParticle> VortexEmits { get => _vortexCluster.Emits; }
        public List<TracerParticle> TracerEmits { get => _tracerCluster.Emits; }
        public List<HeatFieldEdit> HeatFieldEdits { get => _heatField.Edits; }


        void Awake()
        {
            _heatField = new(HeatFieldShader, Bounds, HeatFieldMaxGridSize, HeatFieldMaxEditCount);
            _vortexCluster = new(VortexComputeShader, _heatField, MaxVortexParticleCount, MaxVortexEmitCount);
            _tracerCluster = new(ParticleMaterial, TracerComputeShader, _vortexCluster, MaxTracerParticleCount, MaxTracerEmitCount);
        }

        void OnDisable()
        {
            _vortexCluster.Destroy();
            _vortexCluster = null;
            _tracerCluster.Destroy();
            _tracerCluster = null;
            _heatField.Destroy();
            _heatField = null;
        }

        void Update()
        {
            /* float x = UnityEngine.Random.Range(-0.2f, 0.2f);
            float y = UnityEngine.Random.Range(-0.2f, 0.2f);
            float z = UnityEngine.Random.Range(-0.2f, 0.2f);
            _tracerCluster.Emits.Add(new TracerParticle(new Vector3(x, y, z), 1.0f)); */
            
            _heatField.Edit();

            _vortexCluster.Emit(_flip);
            _tracerCluster.Emit(_flip);

            _vortexCluster.Simulate(_flip, Time.deltaTime);
            _tracerCluster.Simulate(_flip, !_flip, Time.deltaTime);
            _tracerCluster.Draw(!_flip, Bounds);

            _flip = !_flip;
        }
        
        #if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            Matrix4x4 prev_matrix = Gizmos.matrix;
            Color prev_color = Gizmos.color;
            
            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);

            Gizmos.color = prev_color;
            Gizmos.matrix = prev_matrix;
        }
        #endif
    }

}