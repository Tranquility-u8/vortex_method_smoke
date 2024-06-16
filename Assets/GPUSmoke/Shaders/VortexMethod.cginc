#ifndef VORTEX_METHOD_CGINC
#define VORTEX_METHOD_CGINC

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

#define _VM_INV_EPSILON2(PREFIX) float(u##PREFIX##InvEpsilon2)
#define _VM_HEAT_BUOYANCY_FACTOR(PREFIX) float(u##PREFIX##HeatBuoyancyFactor)

#define _VM_DEF_UNIFORM(PREFIX) float u##PREFIX##InvEpsilon2; float u##PREFIX##HeatBuoyancyFactor;
#define _VM_DEF_FUNC(PREFIX) \
float3 velocity_from_pos_vor(in const float3 pi_pos, in const PosVor pj_pos_vor) { \
    float3 d = pi_pos - pj_pos_vor.pos; \
    float r_ij2 = dot(d, d), r_ij = sqrt(r_ij2), r_ij3 = r_ij2 * r_ij; \
    float factor = (1.0 - exp(-r_ij3 * _VM_INV_EPSILON2(PREFIX))) / (4.0 * 3.1415926 * r_ij3); \
    factor = isnan(factor) || isinf(factor) ? 0 : factor; \
    return cross(pj_pos_vor.vor, d) * factor; \
}

#endif