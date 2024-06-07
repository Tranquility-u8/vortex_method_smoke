using System.Collections.Generic;
using UnityEngine;

namespace VortexMethod 
{
    public class TestSmokeSystem : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] [Range(0f, 1f)] private float Dt = 0.1f; //ʱ�䲽��
        private int NUM_VORTEX;
        [SerializeField] private int NUM_TRACER;
        [SerializeField] private List<ParticleConfig> vortex_particle_configs;


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

        #region Init
        void Init()
        {
            NUM_VORTEX = vortex_particle_configs.Count;

            InitVortex();
            InitTracer();
        }

        void InitVortex()
        {
            var smoke_system = GetComponent<SmokeSystem>();
            for (int i = 0; i < NUM_VORTEX; i++)
                smoke_system.VortexEmits.Enqueue(new global::VortexParticle(vortex_particle_configs[i].pos, vortex_particle_configs[i].vor, float.PositiveInfinity));
        }

        void InitTracer()
        {
            var smoke_system = GetComponent<SmokeSystem>();
            for (int i = 0; i < NUM_TRACER; i++)
            {
                float x = Random.Range(-0.5f, 0.5f);
                float y = Random.Range(-1.5f, 1.5f);
                float z = Random.Range(-0.5f, 0.5f);
                smoke_system.TracerEmits.Enqueue(new global::TracerParticle(new Vector3(x, y, z), float.PositiveInfinity));
            }
        }
        #endregion
    }
}


