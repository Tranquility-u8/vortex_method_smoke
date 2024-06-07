// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles

#define EPS 0.02
float3 velocity_from_pos_vor(in const float3 pi_pos, in const float2x3 pj_pos_vor) {
    float3 d = pi_pos - pj_pos_vor[0];
    float r_ij = length(d);
    float r_ij3 = r_ij * r_ij * r_ij;
    float factor = 1.0 / (4.0 * 3.1415926 * r_ij3) * (1.0 - exp(-r_ij3 / (EPS * EPS)));
    factor = isnan(factor) || isinf(factor) ? 0 : factor;
    return cross(pj_pos_vor[1], d) * factor;
}
#undef EPS