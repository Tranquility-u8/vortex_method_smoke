#ifndef PARTICLE_HASH_GRID_CGINC
#define PARTICLE_HASH_GRID_CGINC

#include "Grid.cginc"

#define __PHG_RANGES(PREFIX) u##PREFIX##Ranges
#define __PHG_PARTICLE_IDS(PREFIX) u##PREFIX##ParticleIDs

#define _PHG_DEF_UNIFORM(PREFIX) _GRID_DEF_UNIFORM(PREFIX)
#define _PHG_DEF_BUFFER(PREFIX) \
    StructuredBuffer<uint2> __PHG_RANGES(PREFIX); \
    StructuredBuffer<uint> __PHG_PARTICLE_IDS(PREFIX);

#define _PHG_REDUCE_NEIGHBOUR(PREFIX, WORLD, REDUCER) [unroll] do { \
    int3 __phg_cell = _GRID_WORLD2CELL(PREFIX, INT_FLOOR, WORLD); \
    [unroll] for (int __phg_dz = -1; __phg_dz <= 1; ++__phg_dz) \
        [unroll] for (int __phg_dy = -1; __phg_dy <= 1; ++__phg_dy) \
            [unroll] for (int __phg_dx = -1; __phg_dx <= 1; ++__phg_dx) { \
                int3 __phg_d_cell = __phg_cell + int3(__phg_dx, __phg_dy, __phg_dz); \
                if (_GRID_CELL_IN_GRID_INT(PREFIX, __phg_d_cell)) { \
                    uint2 __phg_r = __PHG_RANGES(PREFIX)[_GRID_CELL2ID(PREFIX, __phg_d_cell)]; \
                    for (uint __phg_i = __phg_r.x; __phg_i != __phg_r.y; ++__phg_i) { \
                        uint __phg_p_id = __PHG_PARTICLE_IDS(PREFIX)[__phg_i]; \
                        REDUCER(__phg_p_id); \
                    } \
                } \
            } \
    } while (false)

#endif