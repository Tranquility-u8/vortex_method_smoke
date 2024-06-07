#define __PC_MAX_COUNT(PREFIX) u##PREFIX##MaxParticleCount
#define __PC_COUNTS(PREFIX) u##PREFIX##ParticleCount
#define __PC_COUNTS_RW(PREFIX) u##PREFIX##ParticleCount_RW
#define __PC_FLIP(PREFIX) u##PREFIX##Flip
#define __PC_PARTICLES(PREFIX) u##PREFIX##Particles
#define __PC_PARTICLES_RW(PREFIX) u##PREFIX##Particles_RW

#define _PC_SRC_IDX(PREFIX, IDX) (__PC_FLIP(PREFIX) == 0 ? (IDX) : (IDX) + __PC_MAX_COUNT(PREFIX))
#define _PC_DST_IDX(PREFIX, IDX) (__PC_FLIP(PREFIX) == 1 ? (IDX) : (IDX) + __PC_MAX_COUNT(PREFIX))

#define _PC_REF_SRC_COUNT(PREFIX) (__PC_COUNTS_RW(PREFIX)[__PC_FLIP(PREFIX)])
#define _PC_RAW_SRC_COUNT(PREFIX) (__PC_COUNTS(PREFIX)[__PC_FLIP(PREFIX)])
#define _PC_SRC_COUNT(PREFIX) (min(_PC_RAW_SRC_COUNT(PREFIX), __PC_MAX_COUNT(PREFIX)))

#define _PC_REF_DST_COUNT(PREFIX) (__PC_COUNTS_RW(PREFIX)[__PC_FLIP(PREFIX) ^ 1u])
#define _PC_RAW_DST_COUNT(PREFIX) (__PC_COUNTS(PREFIX)[__PC_FLIP(PREFIX) ^ 1u])
#define _PC_DST_COUNT(PREFIX) (min(_PC_RAW_DST_COUNT(PREFIX), __PC_MAX_COUNT(PREFIX)))

#define _PC_SRC_UNSAFE_GET(PREFIX, IDX) (__PC_PARTICLES(PREFIX)[_PC_SRC_IDX(PREFIX, IDX)])
#define _PC_DST_UNSAFE_GET(PREFIX, IDX) (__PC_PARTICLES(PREFIX)[_PC_DST_IDX(PREFIX, IDX)])

#define _PC_SRC_SAFE_GET(PREFIX, IDX) (__PC_PARTICLES(PREFIX)[_PC_SRC_IDX(PREFIX, min(IDX, __PC_MAX_COUNT(PREFIX) - 1u))])
#define _PC_DST_SAFE_GET(PREFIX, IDX) (__PC_PARTICLES(PREFIX)[_PC_DST_IDX(PREFIX, min(IDX, __PC_MAX_COUNT(PREFIX) - 1u))])

#define _PC_SRC_SAFE_PUSH(PREFIX, P) [unroll] do { \
    uint idx; \
    InterlockedAdd(_PC_REF_SRC_COUNT(PREFIX), 1u, idx); \
    if (idx >= __PC_MAX_COUNT(PREFIX)) \
        break; \
    __PC_PARTICLES_RW(PREFIX)[_PC_SRC_IDX(PREFIX, idx)] = (P); \
} while (false)

#define _PC_SRC_UNSAFE_PUSH(PREFIX, P) [unroll] do { \
    uint idx; \
    InterlockedAdd(_PC_REF_SRC_COUNT(PREFIX), 1u, idx); \
    __PC_PARTICLES_RW(PREFIX)[_PC_SRC_IDX(PREFIX, idx)] = (P); \
} while (false)

#define _PC_DST_SAFE_PUSH(PREFIX, P) [unroll] do { \
    uint idx; \
    InterlockedAdd(_PC_REF_DST_COUNT(PREFIX), 1u, idx); \
    if (idx >= __PC_MAX_COUNT(PREFIX)) \
        break; \
    __PC_PARTICLES_RW(PREFIX)[_PC_DST_IDX(PREFIX, idx)] = (P); \
} while (false)

#define _PC_DST_UNSAFE_PUSH(PREFIX, P) [unroll] do { \
    uint idx; \
    InterlockedAdd(_PC_REF_DST_COUNT(PREFIX), 1u, idx); \
    __PC_PARTICLES_RW(PREFIX)[_PC_DST_IDX(PREFIX, idx)] = (P); \
} while (false)

#define _PC_IDX(PREFIX, F, X) _PC_##F##_IDX(PREFIX, X)
#define _PC_COUNT(PREFIX, F) _PC_##F##_COUNT(PREFIX)
#define _PC_PUSH(PREFIX, F, P) _PC_##F##_PUSH(PREFIX, P)
#define _PC_GET(PREFIX, F, X) _PC_##F##_GET(PREFIX, X)

#define PC_IDX(F, X) _PC_IDX(, F, X)
#define PC_COUNT(F) _PC_COUNT(, F)
#define PC_PUSH(F, P) _PC_PUSH(, F, P)
#define PC_GET(F, X) _PC_GET(, F, X)