using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GPUSmoke
{
    public enum SmokeType
    {
        Normal,
        Mushroom
    }

    public class SmokeSource : MonoBehaviour
    {
        public SmokeSystem SmokeSystem;

        [Header("Heat")] public float Heat = 100.0f;
        public float HeatStddev = 1.0f;

        [Header("Spawn")] public float VortexSpawnRadius = 1.0f;
        public float VortexSpawnTime = 0.02f;
        public float TracerSpawnRadius = 0.5f;
        public float TracerSpawnTime = 0.001f;

        [Header("Vorticity")] public float MinVorticity = 0.5f;
        public float MaxVorticity = 20.0f;

        [Header("Lifespan")] public float MinLifespan = 5.0f;
        public float MaxLifespan = 10.0f;

        [Header("SmokeType")] public SmokeType SmokeType = SmokeType.Normal;
        public float scaleSpawn = 3.0f;
        public float vortexSpawnRadius = 1.0f;
        public float vorScale = 0.75f;
        public float tracerOffset = -1.0f;
        
        [System.Serializable]
        public class ParticleConfig 
        {
            public Vector3 pos;
            public Vector3 vor;
        }
        [SerializeField] public List<ParticleConfig> vortex_particle_configs;
        
        public int NUM_TRACER=10000;
        


        // Maintain Heat
        private HeatFieldEntryID _heatEntryID;

        // Timer for Spawner
        private float _vortexTimer = 0, _tracerTimer = 0;

        void Start()
        {
            _heatEntryID = SmokeSystem.HeatField.AddEntry(new HeatFieldEntry(transform.position, Heat, HeatStddev));
            if (SmokeType == SmokeType.Mushroom)
            {
                var NUM_VORTEX = vortex_particle_configs.Count;

                for (int i = 0; i < NUM_VORTEX; i++)
                    SmokeSystem.VortexEmits.Add(new VortexParticle(transform.position + vortex_particle_configs[i].pos*vortexSpawnRadius, vortex_particle_configs[i].vor*vorScale, MaxLifespan));

                for (int i = 0; i < NUM_TRACER; i++)
                {
                    Vector3 spawnPos = transform.position + Random.insideUnitSphere * scaleSpawn;
                    spawnPos.y *= 0.5f;
                    spawnPos.y += tracerOffset;
                    SmokeSystem.TracerEmits.Add(new TracerParticle(spawnPos, MaxLifespan));
                }
            }
        }

        public void OnDestroy()
        {
            SmokeSystem?.HeatField?.RemoveEntry(_heatEntryID);
        }

        private void MaintainHeat()
        {
            SmokeSystem.HeatField.SetEntry(_heatEntryID, new HeatFieldEntry(transform.position, Heat, HeatStddev));
        }

        private void Spawn_(ref float timer, float spawn_time, float delta_time, Action spawner)
        {
            timer += delta_time;
            if (timer >= spawn_time)
            {
                var count = (int)(timer / spawn_time);
                timer -= count * spawn_time;
                for (int i = 0; i < count; ++i)
                    spawner();
            }
        }

        private void SpawnVortex_()
        {
            var pos = UnityEngine.Random.insideUnitSphere * VortexSpawnRadius;
            var vor = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(MinVorticity, MaxVorticity);
            var life = UnityEngine.Random.Range(MinLifespan, MaxLifespan);
            SmokeSystem.VortexEmits.Add(new VortexParticle(transform.position + pos, vor, life));
        }

        private void SpawnVortex_mushroom()
        {
            var posXY = UnityEngine.Random.insideUnitCircle * VortexSpawnRadius;

            var pos = new Vector3(posXY.x, 0, posXY.y);
            //let vor = up cross pos
            var vor = Vector3.Cross(pos,Vector3.up).normalized * UnityEngine.Random.Range(MinVorticity, MaxVorticity);
            var life = UnityEngine.Random.Range(MinLifespan, MaxLifespan);
            SmokeSystem.VortexEmits.Add(new VortexParticle(transform.position + pos, vor, life));


        }

        private void SpawnTracer_()
        {
            var pos = UnityEngine.Random.insideUnitSphere * TracerSpawnRadius;
            var life = UnityEngine.Random.Range(MinLifespan, MaxLifespan);
            SmokeSystem.TracerEmits.Add(new TracerParticle(transform.position + pos, life));
        }

        void Update()
        {
            MaintainHeat();
            if (SmokeType == SmokeType.Normal)
            {
                Spawn_(ref _vortexTimer, VortexSpawnTime, Time.deltaTime, SpawnVortex_);
                Spawn_(ref _tracerTimer, TracerSpawnTime, Time.deltaTime, SpawnTracer_);
                
            }
            

        }

        IEnumerator Destroy()
        {
            yield return new WaitForSeconds(this.MaxLifespan);
            Destroy(this.gameObject);
        }

    }

}