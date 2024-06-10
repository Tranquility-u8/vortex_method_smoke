using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSmoke
{
    public class TestSmokeSystemPerformance : MonoBehaviour
    {
        public SmokeSystem SmokeSystem;
        public int Count;
        
        // Start is called before the first frame update
        void Start()
        {
            for (int i = 0; i < Count; ++i) {
                var obj = new GameObject("source " + i);
                obj.transform.SetParent(transform);
                obj.transform.position = new Vector3(
                    UnityEngine.Random.Range(SmokeSystem.Bounds.min.x, SmokeSystem.Bounds.max.x),
                    UnityEngine.Random.Range(SmokeSystem.Bounds.min.y, SmokeSystem.Bounds.max.y),
                    UnityEngine.Random.Range(SmokeSystem.Bounds.min.z, SmokeSystem.Bounds.max.z)
                );
                var ss = obj.AddComponent<SmokeSource>();
                ss.SmokeSystem = SmokeSystem;
                ss.Heat = 50;
                ss.VortexSpawnTime = 0.05f;
                ss.TracerSpawnTime = 0.001f;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}