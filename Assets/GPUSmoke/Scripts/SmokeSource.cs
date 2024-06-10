using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace GPUSmoke
{
    public class SmokeSource : MonoBehaviour
    {
        public SmokeSystem SmokeSystem;

        [Header("Heat")]
        public float Heat = 100.0f;
        public float HeatStddev = 1.0f;

        [Header("Spawn")]
        public float VortexSpawnRadius = 1.0f;
        public float VortexSpawnTime = 0.02f;
        public float TracerSpawnRadius = 0.5f;
        public float TracerSpawnTime = 0.001f;

        [Header("Vorticity")]
        public float MinVorticity = 0.5f;
        public float MaxVorticity = 20.0f;

        [Header("Lifespan")]
        public float MinLifespan = 5.0f;
        public float MaxLifespan = 10.0f;


        // Maintain Heat
        private HeatFieldEntryID _heatEntryID;

        // Timer for Spawner
        private float _vortexTimer = 0, _tracerTimer = 0;

        void Start()
        {
            _heatEntryID = SmokeSystem.HeatField.AddEntry(new HeatFieldEntry(transform.position, Heat, HeatStddev));
        }

        public void OnDestroy() {
            SmokeSystem.HeatField.RemoveEntry(_heatEntryID);
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

        private void SpawnTracer_()
        {
            var pos = UnityEngine.Random.insideUnitSphere * TracerSpawnRadius;
            var life = UnityEngine.Random.Range(MinLifespan, MaxLifespan);
            SmokeSystem.TracerEmits.Add(new TracerParticle(transform.position + pos, life));
        }

        void Update()
        {
            MaintainHeat();
            Spawn_(ref _vortexTimer, VortexSpawnTime, Time.deltaTime, SpawnVortex_);
            Spawn_(ref _tracerTimer, TracerSpawnTime, Time.deltaTime, SpawnTracer_);
        }
    }
}