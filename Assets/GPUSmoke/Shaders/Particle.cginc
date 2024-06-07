// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles

struct VortexParticle {
    float3 pos, vor; 
    float life;
};

struct TracerParticle {
    float3 pos; 
    float life;
};

float2x3 pos_vor_from_vortex_particle(in const VortexParticle p) {
    return float2x3(p.pos, p.vor);
}