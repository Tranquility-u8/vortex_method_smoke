#ifndef PARTICLE_CLUSTER_CGINC
#define PARTICLE_CLUSTER_CGINC

#define __PC_MAX_COUNT(PREFIX) uint(u##PREFIX##MaxCount)
#define __PC_COUNT(PREFIX) uint(u##PREFIX##Count)
#define __PC_PUSH_COUNT(PREFIX) u##PREFIX##PushCount_RW[0]
#define __PC_FLIP(PREFIX) uint(u##PREFIX##Flip)
#define __PC_PARTICLES(PREFIX) u##PREFIX##Particles
#define __PC_PARTICLES_RW(PREFIX) u##PREFIX##Particles_RW

#define _PC_DEF_UNIFORM(PREFIX) \
    uint u##PREFIX##MaxCount, u##PREFIX##Count, u##PREFIX##Flip;

#define _PC_DEF_BUFFER(PREFIX, TYPE) \
    StructuredBuffer<TYPE> u##PREFIX##Particles;

#define _PC_DEF_BUFFER_RW(PREFIX, TYPE) \
    RWStructuredBuffer<uint> u##PREFIX##PushCount_RW; \
    RWStructuredBuffer<TYPE> u##PREFIX##Particles_RW;

#define __PC_IDX(PREFIX, IDX, SRC0DST1) (__PC_FLIP(PREFIX) == SRC0DST1 ? (IDX) : (IDX) + __PC_MAX_COUNT(PREFIX))

#define _PC_UNSAFE_GET(PREFIX, IDX) (__PC_PARTICLES(PREFIX)[_PC_IDX(PREFIX, IDX)])
#define _PC_SAFE_GET(PREFIX, IDX) (__PC_PARTICLES(PREFIX)[_PC_IDX(PREFIX, min(IDX, __PC_MAX_COUNT(PREFIX) - 1u))])

#define _PC_SAFE_PUSH(PREFIX, P) [unroll] do { \
    uint __pc_idx; \
    InterlockedAdd(__PC_PUSH_COUNT(PREFIX), 1u, __pc_idx); \
    if (__pc_idx >= __PC_MAX_COUNT(PREFIX)) \
        break; \
    __PC_PARTICLES_RW(PREFIX)[__PC_IDX(PREFIX, __pc_idx, 1)] = (P); \
} while (false)

#define _PC_UNSAFE_PUSH(PREFIX, P) [unroll] do { \
    uint __pc_idx; \
    InterlockedAdd(__PC_PUSH_COUNT(PREFIX), 1u, __pc_idx); \
    __PC_PARTICLES_RW(PREFIX)[__PC_IDX(PREFIX, __pc_idx, 1)] = (P); \
} while (false)

#define _PC_IDX(PREFIX, X) __PC_IDX(PREFIX, X, 0)
#define _PC_COUNT(PREFIX) __PC_COUNT(PREFIX)
#define _PC_PUSH(PREFIX, F, P) _PC_##F##_PUSH(PREFIX, P)
#define _PC_GET(PREFIX, F, X) _PC_##F##_GET(PREFIX, X)

#define PC_DEF_UNIFORM _PC_DEF_UNIFORM()
#define PC_DEF_BUFFER(TYPE) _PC_DEF_BUFFER(, TYPE)
#define PC_DEF_BUFFER_RW(TYPE) _PC_DEF_BUFFER_RW(, TYPE)
#define PC_IDX(X) _PC_IDX(, X)
#define PC_COUNT _PC_COUNT()
#define PC_PUSH(F, P) _PC_PUSH(, F, P)
#define PC_GET(F, X) _PC_GET(, F, X)

#endif