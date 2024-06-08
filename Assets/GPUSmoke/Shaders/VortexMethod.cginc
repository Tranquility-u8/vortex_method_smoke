#define VM_EPS 0.02
#define VM_HEAT_BUOYANCY_FACTOR 0.0001
#define VM_HEAT_CURL_CELL_DELTA 0.5

struct VortexParticle {
    float3 pos, vor; 
    float life;
};

struct TracerParticle {
    float3 pos; 
    float life;
};

struct PosVor {
    float3 pos, vor; 
};

PosVor pos_vor_from_vortex_particle(in const VortexParticle p) {
    PosVor pos_vor;
    pos_vor.pos = p.pos;
    pos_vor.vor = p.vor;
    return pos_vor;
}

float3 velocity_from_pos_vor(in const float3 pi_pos, in const PosVor pj_pos_vor) {
    float3 d = pi_pos - pj_pos_vor.pos;
    float r_ij2 = dot(d, d), r_ij = sqrt(r_ij2), r_ij3 = r_ij2 * r_ij;
    float factor = (1.0 - exp(-r_ij3 / (VM_EPS * VM_EPS))) / (4.0 * 3.1415926 * r_ij3);
    factor = isnan(factor) || isinf(factor) ? 0 : factor;
    return cross(pj_pos_vor.vor, d) * factor;
}