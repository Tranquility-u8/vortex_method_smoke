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
                _smokeSystem.VortexEmits.Add(new VortexParticle(transform.position + vortex_particle_configs[i].pos, vortex_particle_configs[i].vor, _life));

            for (int i = 0; i < NUM_TRACER; i++)
            {
                float x = transform.position.x + Random.Range(-1.5f, 1.5f);
                float y = transform.position.y + Random.Range(-0.5f, 0.5f);
                float z = transform.position.z + Random.Range(-1.5f, 1.5f);
                _smokeSystem.TracerEmits.Add(new TracerParticle(new Vector3(x, y, z), _life));
            }
            
            // _smokeSystem.HeatField.AddEntry(new HeatFieldEntry(Vector3.zero, 300.0f, 2.0f));
        }
    }
}


