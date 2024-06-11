using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace GPUSmoke
{
    public class SmokeSystem : MonoBehaviour
    {
        public Bounds Bounds;
        public float TimeScale = 1.0f;

        [Header("Particle")]
        public ComputeShader VortexComputeShader;
        public ComputeShader TracerComputeShader;
        public int MaxVortexParticleCount;
        public int MaxTracerParticleCount;
        public Material ParticleMaterial;
        
        [Header("Heat Field")]
        public ComputeShader HeatFieldShader;
        public int HeatFieldMaxGridSize;
        public int HeatFieldMaxEntryCount;

        [Header("Vortex Hash Grid")]
        public ComputeShader VortexHashGridComputeShader;
        public int VortexHashGridMaxGridSize;

        [Header("Vortex Method")]
        public float Epsilon;
        public float HeatBuoyancyFactor;

        private VortexParticleCluster _vortexCluster;
        private TracerParticleCluster _tracerCluster;
        private HeatField _heatField;
        private ParticleHashGrid<float, VortexParticle> _vortexHashGrid;
        private ParticleDrawer<float, TracerParticle> _tracerDrawer;
        private bool _flip = true;

        public List<VortexParticle> VortexEmits { get => _vortexCluster.Emits; }
        public List<TracerParticle> TracerEmits { get => _tracerCluster.Emits; }
        public HeatField HeatField { get => _heatField; }


        void Awake()
        {
            VortexMethodConfig vm_config = new() {
                epsilon = Epsilon,
                heat_buoyancy_factor = HeatBuoyancyFactor
            };
            _heatField = new(HeatFieldShader, Bounds, HeatFieldMaxGridSize, HeatFieldMaxEntryCount);
            _vortexCluster = new(VortexComputeShader, _heatField, vm_config, MaxVortexParticleCount);
            _tracerCluster = new(TracerComputeShader, _heatField, vm_config, _vortexCluster, MaxTracerParticleCount);
            _vortexHashGrid = new(VortexHashGridComputeShader, _vortexCluster, Bounds, VortexHashGridMaxGridSize);
            _tracerDrawer = new(ParticleMaterial, _tracerCluster, Bounds);
        }

        void OnDisable()
        {
            _vortexCluster.Destroy();
            _vortexCluster = null;
            _tracerCluster.Destroy();
            _tracerCluster = null;
            _heatField.Destroy();
            _heatField = null;
            _vortexHashGrid.Destroy();
            _vortexHashGrid = null;
            _tracerDrawer = null;
        }

        void Update()
        {
            /* float x = UnityEngine.Random.Range(-0.2f, 0.2f);
            float y = UnityEngine.Random.Range(-0.2f, 0.2f);
            float z = UnityEngine.Random.Range(-0.2f, 0.2f);
            _tracerCluster.Emits.Add(new TracerParticle(new Vector3(x, y, z), 1.0f)); */
            
            _heatField.Update();

            float time_step = Time.deltaTime * TimeScale;

            bool vortex_flip = _flip;
            int vortex_count = 0;
            _vortexCluster.Simulate(_flip, time_step, (bool flip, int count) => {
                vortex_flip = flip;
                vortex_count = count;
            });
            _tracerCluster.Simulate(_flip, vortex_flip, vortex_count, time_step, (bool flip, int count) => {
                _tracerDrawer.Draw(flip, count);
            });

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