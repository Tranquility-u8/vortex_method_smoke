using System.Collections.Generic;
using UnityEngine;


public class ParticleUpdater : MonoBehaviour
{

    [SerializeField] private GameObject prefab;

    private float Dt = 0.1f;
    [SerializeField] private float EPS;
    [SerializeField] private int NUM_VORTEX;
    [SerializeField] private int NUM_TRACER;

    private List<Particle> vortex_particles;
    private List<Particle> tracer_particles;

    private List<Particle> tmp_vortex_particles;

    private void Awake()
    {
        InitVortex();
        InitTracer();
    }


    private void Update()
    {
        UpdateVortex();
        UpdateTracer();

        if (Input.GetKeyDown(KeyCode.Return))
        {
#if UNITY_EDITOR
            //Debug.Log("==========²½½ø==========");
#endif

        }

    }

    #region Init
    void InitVortex()
    {
        vortex_particles = new List<Particle>();
        vortex_particles.Add(new VortexParticle(new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f)));
        vortex_particles.Add(new VortexParticle(new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 1f)));

        tmp_vortex_particles = new List<Particle>();
        tmp_vortex_particles.Add(new VortexParticle(new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f)));
        tmp_vortex_particles.Add(new VortexParticle(new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 1f)));
    }

    void InitTracer()
    {
        tracer_particles = new List<Particle>();
        for(int i = 0; i < NUM_TRACER; i++)
        {
            float x = Random.Range(-0.5f, 0.5f);
            float y = Random.Range(-1.5f, 1.5f);
            float z = Random.Range(-0.5f, 0.5f);
            tracer_particles.Add(new TracerParticle(new Vector3(x, y, z), prefab));
        }
    }
    #endregion

    #region Update
    void UpdateVortex()
    {
        for(int i = 0; i < NUM_VORTEX; i++)
        {
            tmp_vortex_particles[i] = vortex_particles[i];
        }

        for(int i = 0; i < NUM_VORTEX; i++)
        {
            Vector3 v = new Vector3(0f, 0f, 0f);
            for(int j = 0; j < NUM_VORTEX; j++)
            {
                if (i == j) continue;
                v += compute_v_from_single_vortex(vortex_particles[i], tmp_vortex_particles[j]);
            }
            vortex_particles[i].data.pos += v * Dt;
        }
    }

    void UpdateTracer()
    {
        for(int i = 0; i < NUM_TRACER; i++)
        {
            rk3_integrate_pos(tracer_particles[i], Dt);
            tracer_particles[i].UpdatePos();
        } 
    }
    #endregion

    #region HelpFunc
    Vector3 compute_v_from_single_vortex(Particle pi, Particle pj)
    {
        Vector3 v = new Vector3(0, 0, 0);

        Vector3 pi_pos = pi.data.pos;
        Vector3 pj_vor = pj.data.vor;
        Vector3 pj_pos = pj.data.pos;

        float dx = pi_pos.x - pj_pos.x;
        float dy = pi_pos.y - pj_pos.y;
        float dz = pi_pos.z - pj_pos.z;

        float r_ij = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        float r_ij3 = r_ij * r_ij * r_ij;

        float factor = (float)(1 / (4 * 3.1415926 * r_ij3) * (1.0 - Mathf.Exp(-r_ij3 / (EPS * EPS))));

        v.x = (pj_vor.y * dz - pj_vor.z * dy) * factor;
        v.y = (pj_vor.z * dx - pj_vor.x * dz) * factor;
        v.z = (pj_vor.x * dy - pj_vor.y * dx) * factor;
        
        return v;
    }

    Vector3 compute_v_from_all_vortex(Particle pi, List<Particle> pjs)
    {
        Vector3 v = new Vector3(0, 0, 0);
        for (int j = 0; j < pjs.Count; j++)
        {
            v += compute_v_from_single_vortex(pi, pjs[j]);
        }
        return v;
    }

    void rk3_integrate_pos(Particle p, float dt)
    {
        Vector3 pos0 = p.data.pos;

        Vector3 v = compute_v_from_all_vortex(p, vortex_particles);
        pos0 += v * dt;
        p.data.pos = pos0;

    }
    #endregion
}