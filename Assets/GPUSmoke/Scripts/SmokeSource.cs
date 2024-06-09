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
        private Vector3 _prevHeatPos;
        private float _prevHeat, _prevHeatStddev;

        // Timer for Spawner
        private float _vortexTimer = 0, _tracerTimer = 0;

        void Start()
        {
            SetHeat();
        }

        /* void OnDisable()
        {
            SmokeSystem?.HeatFieldEdits?.Add(new HeatFieldEdit(transform.position, -Heat, HeatStddev));
        } */

        private void SetHeat()
        {
            _prevHeatPos = transform.position;
            _prevHeat = Heat;
            _prevHeatStddev = HeatStddev;
            SmokeSystem.HeatFieldEdits.Add(new HeatFieldEdit(transform.position, Heat, HeatStddev));
        }
        
        private void MaintainHeat()
        {
            if (Heat != _prevHeat || HeatStddev != _prevHeatStddev || transform.position != _prevHeatPos)
            {
                // Remove Previous Heat
                SmokeSystem.HeatFieldEdits.Add(new HeatFieldEdit(_prevHeatPos, -_prevHeat, _prevHeatStddev));
                // Set New Heat
                SetHeat();
            }
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
            var vor = Vector3.Cross(Vector3.up, new Vector3(pos.x, pos.y, pos.z));
            vor = Vector3.Normalize(vor) * UnityEngine.Random.Range(MinVorticity, MaxVorticity);
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