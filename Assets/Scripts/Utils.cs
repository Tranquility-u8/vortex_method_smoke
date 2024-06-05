using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VortexMethod
{
    public enum ParticleType
    {
        VORTEX_PARTICLE,
        TRACER_PARTICLE
    }

    public class ParticleData
    {
        //public int index; 
        public Vector3 pos;
        public Vector3 vor;

        public ParticleData()
        {
            pos = new Vector3(0f, 0f, 0f);
            vor = new Vector3(0f, 0f, 0f);
        }
    }

    public abstract class Particle
    {
        public ParticleType type;
        public ParticleData data;

        public Particle()
        {
            data = new ParticleData();
        }

        public virtual void UpdatePos() { }
    }


    public class VortexParticle : Particle
    {
        public VortexParticle(Vector3 _pos, Vector3 _vor)
        {
            data.pos = _pos;
            data.vor = _vor;
        }

        public VortexParticle(VortexParticle other)
        {
            data.pos = other.data.pos;
            data.vor = other.data.vor;
        }
    }

    public class TracerParticle : Particle
    {
        private GameObject sprite;

        public TracerParticle(Vector3 pos, GameObject prefab, Transform parent)
        {
            data.pos = pos;
            sprite = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
            sprite.AddComponent<LookAtCamera>();
        }

        public override void UpdatePos()
        {
            sprite.transform.localPosition = data.pos;
        }

    }
}

