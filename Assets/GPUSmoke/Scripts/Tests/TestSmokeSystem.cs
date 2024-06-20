using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke
{
    public class TestSmokeSystem : MonoBehaviour
    {
        [Header("Parameters")]
        private int NUM_VORTEX;
        [SerializeField] private int NUM_TRACER;
        [SerializeField] private List<ParticleConfig> vortex_particle_configs;
        [SerializeField] private SmokeSystem _smokeSystem;
        [SerializeField] private float _life = 100.0f;
        public float scaleSpawn = 3.0f;
        public float vortexSpawnRadius = 1.0f;
        public float vorScale = 0.75f;
        public float tracerOffset = -1.0f;

        [System.Serializable]
        private class ParticleConfig 
        {
            public Vector3 pos;
            public Vector3 vor;
        }
        
        
        private bool _firstUpdate = true;

        private void Start()
        {
        }


        private void Update()
        {
            if (_firstUpdate)
                Init();
            _firstUpdate = false;
        }

        void Init()
        {
            NUM_VORTEX = vortex_particle_configs.Count;

            for (int i = 0; i < NUM_VORTEX; i++)
                _smokeSystem.VortexEmits.Add(new VortexParticle(transform.position + vortex_particle_configs[i].pos*vortexSpawnRadius, vortex_particle_configs[i].vor*vorScale, _life));

            for (int i = 0; i < NUM_TRACER; i++)
            {
                Vector3 spawnPos = Random.insideUnitSphere * scaleSpawn;
                spawnPos.y *= 1.2f;
                spawnPos.y += tracerOffset;
                _smokeSystem.TracerEmits.Add(new TracerParticle(transform.position + spawnPos, _life));
            }
            
            // _smokeSystem.HeatField.AddEntry(new HeatFieldEntry(Vector3.zero, 300.0f, 2.0f));
        }
    }
}


