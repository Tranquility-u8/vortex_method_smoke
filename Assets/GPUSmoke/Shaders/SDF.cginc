#ifndef SDF_CGINC
#define SDF_CGINC

#include "Grid.cginc"

#define __SDF_TEXTURE(PREFIX) u##PREFIX##Texture
#define __SDF_SAMPLER(PREFIX) sampler_u##PREFIX##Texture
#define __SDF_UNIT_DIST(PREFIX) u##PREFIX##UnitDistance

#define _SDF_DEF_UNIFORM(PREFIX) \
    _GRID_DEF_UNIFORM(PREFIX) \
    float __SDF_UNIT_DIST(PREFIX);

#define _SDF_DEF_TEXTURE(PREFIX) \
    Texture3D<float> __SDF_TEXTURE(PREFIX); \
    SamplerState __SDF_SAMPLER(PREFIX);

#define __SDF_SAMPLE_UVW_RAW(PREFIX, UVW) \
    __SDF_TEXTURE(PREFIX).SampleLevel(__SDF_SAMPLER(PREFIX), UVW, 0)

#define __SDF_SAMPLE_UVW(PREFIX, UVW) \
    (__SDF_SAMPLE_UVW_RAW(PREFIX, UVW) * __SDF_UNIT_DIST(PREFIX))

#define __SDF_SAMPLE_WORLD_RAW(PREFIX, WORLD) \
    __SDF_SAMPLE_UVW_RAW(PREFIX, _GRID_WORLD2UVW(PREFIX, WORLD))

#define __SDF_SAMPLE_WORLD(PREFIX, WORLD) \
    (__SDF_SAMPLE_WORLD_RAW(PREFIX, WORLD) * __SDF_UNIT_DIST(PREFIX))

#define _SDF_SAMPLE(PREFIX, F, P) __SDF_SAMPLE_##F(PREFIX, P)


// Normal Calculation Method: https://iquilezles.org/articles/normalsSDF/
#define _SDF_FIX_WORLD_POS(PREFIX, WORLD_VAR) \
    [unroll] do { \
        float3 __sdf_uvw = _GRID_WORLD2UVW(PREFIX, WORLD_VAR); \
        float __sdf_d = _SDF_SAMPLE(PREFIX, UVW, __sdf_uvw); \
        if (__sdf_d < 0.0) { \
            float3 __sdf_uvw_half_d = _GRID_UNIT_CELL2UVW(PREFIX) * 0.5; \
            float2 __sdf_k = float2(1, -1); \
            float3 __sdf_d1 = __sdf_k.xyy * _SDF_SAMPLE(PREFIX, UVW, __sdf_uvw + __sdf_k.xyy * __sdf_uvw_half_d); \
            float3 __sdf_d2 = __sdf_k.yyx * _SDF_SAMPLE(PREFIX, UVW, __sdf_uvw + __sdf_k.yyx * __sdf_uvw_half_d); \
            float3 __sdf_d3 = __sdf_k.yxy * _SDF_SAMPLE(PREFIX, UVW, __sdf_uvw + __sdf_k.yxy * __sdf_uvw_half_d); \
            float3 __sdf_d4 = __sdf_k.xxx * _SDF_SAMPLE(PREFIX, UVW, __sdf_uvw + __sdf_k.xxx * __sdf_uvw_half_d); \
            float3 __sdf_n = __sdf_d1 + __sdf_d2 + __sdf_d3 + __sdf_d4; \
            if (__sdf_n.x != 0 || __sdf_n.y != 0 || __sdf_n.z != 0) \
                __sdf_n = normalize(__sdf_n); \
            WORLD_VAR -= __sdf_d * __sdf_n; \
        } \
    } while(false)

#endif