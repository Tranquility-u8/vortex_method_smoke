using System.Collections.Generic;
using UnityEngine;

namespace VortexMethod 
{
    [HelpURL("https://zhuanlan.zhihu.com/p/425766028")] 
    public class ParticleSystem : MonoBehaviour
    {
        /// <summary>
        /// 
        /// TODO LISTS�
        /// 1�GPU��
        /// 2������
        /// 3������
        /// 4�Treecode��
        /// 
        /// </summary>
        /// 
        [Header("Particle")]
        [SerializeField] [Range(0f, 1f)] private float Dt = 0.1f; //����
        [SerializeField] [Range(0f, 0.1f)] private float EPS; //����
        
        private int NUM_VORTEX;
        [SerializeField] private int NUM_TRACER;
        [SerializeField] private List<ParticleConfig> vortex_particle_configs;
        
        [Header("Heat Field")]
        [SerializeField] private float HEAT_RADIUS;
        [SerializeField] [Range(1, 10)] private float HEAT_X_SCALE;
        [SerializeField] [Range(1, 10)] private float HEAT_Y_SCALE;
        [SerializeField] [Range(1, 10)] private float HEAT_Z_SCALE;
        [SerializeField] [Range(3, 65)] private int HEAT_VERTEX_NUM; //�����
        private float HEAT_GRID_SIZE; //����
        private float HEAT_CENTER_DEGREE = 1f; //����
        [SerializeField] [Range(0, 0.005f)] private float HEAT_BUOYANCY_FACTOR; //����

        [Header("Particle Prefab")]
        [SerializeField] private GameObject prefab;


        [System.Serializable]
        private class ParticleConfig 
        {
            public Vector3 pos;
            public Vector3 vor;
        }
        
        private List<Particle> vortex_particles;
        private List<Particle> tracer_particles;
        private List<Particle> tmp_vortex_particles;

        private List<List<List<float>>> heat_field;
        private List<List<List<Vector2>>> vortex_field;

        private void Awake()
        {
            Init();
        }


        private void Update()
        {
            UpdateParticle();
            /*
            if (Input.GetKeyDown(KeyCode.Return))
            {
#if UNITY_EDITOR
                Debug.Log("==========��==========");
#endif
                UpdateParticle();
            }
            */
        }

        #region Init
        void Init()
        {
            InitParameter();
            InitHeatField();
            InitVortex();
            InitTracer();
        }

        void InitParameter()
        {
            NUM_VORTEX = vortex_particle_configs.Count;
            HEAT_RADIUS = Mathf.Max(1f, HEAT_RADIUS);
            HEAT_X_SCALE = Mathf.Min(1f, HEAT_X_SCALE);
            HEAT_Y_SCALE = Mathf.Min(1f, HEAT_Y_SCALE);
            HEAT_Z_SCALE = Mathf.Min(1f, HEAT_Z_SCALE);

            if (HEAT_VERTEX_NUM % 2 == 0)
                HEAT_VERTEX_NUM++;
            HEAT_VERTEX_NUM = Mathf.Max(3, HEAT_VERTEX_NUM);

            HEAT_GRID_SIZE = HEAT_RADIUS / (HEAT_VERTEX_NUM - 1);
        }

        void InitHeatField()
        {
            int medium = HEAT_VERTEX_NUM / 2;

            // ������/���
            heat_field = new List<List<List<float>>>();
            
            for (int i = 0; i < HEAT_VERTEX_NUM; i++)
            {
                List<List<float>> heat2d = new List<List<float>>();
                for(int j = 0; j < HEAT_VERTEX_NUM; j++)
                {
                    List<float> heat1d = new List<float>();
                    for(int k = 0; k < HEAT_VERTEX_NUM; k++)
                    {
                        float degree = 0;

                        float distance = Mathf.Sqrt((i - medium) * (i - medium) + (j - medium) * (j - medium) + (k - medium) * (k - medium));
                        if(distance >= medium)
                        {
                            degree = 0;
                        }
                        else
                        {
                            degree = (1 - distance / medium) * HEAT_CENTER_DEGREE;
                        }
                        heat1d.Add(degree);

                    }
                    heat2d.Add(heat1d);
                }
                heat_field.Add(heat2d);
            }

            // ������
            // vector.x => x�� vector.y => z��
            vortex_field = new List<List<List<Vector2>>>();

            for (int i = 0; i < HEAT_VERTEX_NUM; i++)
            {
                List<List<Vector2>> vortex2d = new List<List<Vector2>>();
                for (int j = 0; j <HEAT_VERTEX_NUM; j++)
                {
                    List<Vector2> vortex1d = new List<Vector2>();
                    for (int k = 0; k < HEAT_VERTEX_NUM; k++)
                    {
                        vortex1d.Add(new Vector2(0f, 0f));
                    }
                    vortex2d.Add(vortex1d);
                }
                vortex_field.Add(vortex2d);
            }

            // ���������
            // curl(F) = (dFz/dy - dFy/dz) x + (dFx/dz + dFz/dx)y + (dFy/dx + dFx/dy)z 
            // ���������dFx = dFz = 0, ��curl(F) = (dFy/dz)x + (dFy/dx)z
            for (int i = 0; i < HEAT_VERTEX_NUM; i++)
            {

                for(int j = 0; j < HEAT_VERTEX_NUM; j++)
                {
                    for(int k = 0; k < HEAT_VERTEX_NUM; k++)
                    {
                        float curlF_x;
                        float curlF_z;
                        if(k == 0)
                        {
                            curlF_x = heat_field[i][j][k + 1] - heat_field[i][j][k];
                        }
                        else if(k == HEAT_VERTEX_NUM - 1)
                        {
                            curlF_x = heat_field[i][j][k] - heat_field[i][j][k - 1];
                        }
                        else
                        {
                            curlF_x = heat_field[i][j][k + 1] - heat_field[i][j][k - 1];
                        }

                        if(i == 0)
                        {
                            curlF_z = heat_field[i + 1][j][k] - heat_field[i][j][k];
                        }
                        else if(i == HEAT_VERTEX_NUM - 1)
                        {
                            curlF_z = heat_field[i][j][k] - heat_field[i - 1][j][k];
                        }
                        else
                        {
                            curlF_z = heat_field[i + 1][j][k] - heat_field[i - 1][j][k];
                        }

                        vortex_field[i][j][k] = new Vector2(-curlF_x, curlF_z);

                    }

                }

            }
        }

        void InitVortex()
        {
            vortex_particles = new List<Particle>();
            tmp_vortex_particles = new List<Particle>();
            for (int i = 0; i < NUM_VORTEX; i++)
            {
                vortex_particles.Add(new VortexParticle(vortex_particle_configs[i].pos, vortex_particle_configs[i].vor));
                tmp_vortex_particles.Add(new VortexParticle(vortex_particle_configs[i].pos, vortex_particle_configs[i].vor));
            }
        }

        void InitTracer()
        {
            tracer_particles = new List<Particle>();
            for (int i = 0; i < NUM_TRACER; i++)
            {
                float x = Random.Range(-1.5f, 1.5f);
                float y = Random.Range(-0.5f, 0.5f);
                float z = Random.Range(-1.5f, 1.5f);
                tracer_particles.Add(new TracerParticle(new Vector3(x, y, z), prefab, transform));
            }
        }
        #endregion

        #region Update
        void UpdateParticle()
        {
            UpdateVortexParticle();
            UpdateTracerParticle();
        }

        void UpdateVortexParticle()
        {
            int median = HEAT_VERTEX_NUM / 2;

            //�����
            for(int i = 0; i < NUM_VORTEX; i++)
            {
                Vector3 position = vortex_particles[i].data.pos;

                if (Mathf.Abs(position.x) > HEAT_RADIUS
                    || Mathf.Abs(position.y) > HEAT_RADIUS
                    || Mathf.Abs(position.z) > HEAT_RADIUS)
                    continue;

                int x = Mathf.FloorToInt(position.x / HEAT_GRID_SIZE) + median;
                int y = Mathf.FloorToInt(position.y / HEAT_GRID_SIZE) + median;
                int z = Mathf.FloorToInt(position.z / HEAT_GRID_SIZE) + median;
                x = Mathf.Clamp(x, 0, HEAT_VERTEX_NUM - 2);
                y = Mathf.Clamp(y, 0, HEAT_VERTEX_NUM - 2);
                z = Mathf.Clamp(z, 0, HEAT_VERTEX_NUM - 2);

                float left_x = position.x / HEAT_GRID_SIZE + median - x;
                float right_x = 1 - left_x;

                float left_y = position.y / HEAT_GRID_SIZE + median - y;
                float right_y = 1 - left_y;

                float left_z = position.z / HEAT_GRID_SIZE + median - z;
                float right_z = 1 - left_z;

                //���������������
                float increment_x = (
                     right_x * right_y * right_z * vortex_field[x][y][z].x + 
                     right_x * right_y * left_z * vortex_field[x][y][z + 1].x +
                     right_x * left_y * right_z * vortex_field[x][y + 1][z].x + 
                     right_x * left_y * left_z * vortex_field[x][y + 1][z + 1].x +
                     left_x * right_y * right_z * vortex_field[x + 1][y][z].x +
                     left_x * right_y * left_z * vortex_field[x + 1][y][z + 1].x +
                     left_x * left_y * right_z * vortex_field[x + 1][y + 1][z].x +
                     left_x * left_y * left_z * vortex_field[x + 1][y + 1][z + 1].x) * HEAT_BUOYANCY_FACTOR;

                float increment_z = (
                     right_x * right_y * right_z * vortex_field[x][y][z].y +
                     right_x * right_y * left_z * vortex_field[x][y][z + 1].y +
                     right_x * left_y * right_z * vortex_field[x][y + 1][z].y +
                     right_x * left_y * left_z * vortex_field[x][y + 1][z + 1].y +
                     left_x * right_y * right_z * vortex_field[x + 1][y][z].y +
                     left_x * right_y * left_z * vortex_field[x + 1][y][z + 1].y +
                     left_x * left_y * right_z * vortex_field[x + 1][y + 1][z].y +
                     left_x * left_y * left_z * vortex_field[x + 1][y + 1][z + 1].y) * HEAT_BUOYANCY_FACTOR;

                vortex_particles[i].data.vor += new Vector3(increment_x * Dt, 0f, increment_z * Dt);
            }

            //�������
            for (int i = 0; i < NUM_VORTEX; i++)
            {
                tmp_vortex_particles[i] = vortex_particles[i];
            }

            for (int i = 0; i < NUM_VORTEX; i++)
            {
                Vector3 v = new Vector3(0f, 0f, 0f);
                for (int j = 0; j < NUM_VORTEX; j++)
                {
                    if (i == j) continue;
                    v += compute_v_from_single_vortex(vortex_particles[i], tmp_vortex_particles[j]);
                }
                vortex_particles[i].data.pos += v * Dt;
            }
        }

        void UpdateTracerParticle()
        {
            for (int i = 0; i < NUM_TRACER; i++)
            {
                one_order_eular__integrate(tracer_particles[i], Dt);
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

        void one_order_eular__integrate(Particle p, float dt)
        {
            Vector3 pos0 = p.data.pos;

            Vector3 v = compute_v_from_all_vortex(p, vortex_particles);
            pos0 += v * dt;
            p.data.pos = pos0;

        }
        #endregion
    }

}


