#define __SR_R1(NAME, S, X) S(s##NAME[X]);
#define __SR_R2(NAME, S, X) __SR_R1(NAME, S, X); __SR_R1(NAME, S, X + 1);
#define __SR_R4(NAME, S, X) __SR_R2(NAME, S, X); __SR_R2(NAME, S, X + 2);
#define __SR_R8(NAME, S, X) __SR_R4(NAME, S, X); __SR_R4(NAME, S, X + 4);
#define __SR_R16(NAME, S, X) __SR_R8(NAME, S, X); __SR_R8(NAME, S, X + 8);
#define __SR_R32(NAME, S, X) __SR_R16(NAME, S, X); __SR_R16(NAME, S, X + 16);
#define __SR_R64(NAME, S, X) __SR_R32(NAME, S, X); __SR_R32(NAME, S, X + 32);
#define __SR_R128(NAME, S, X) __SR_R64(NAME, S, X); __SR_R64(NAME, S, X + 64);
#define __SR_R256(NAME, S, X) __SR_R128(NAME, S, X); __SR_R128(NAME, S, X + 128);
#define __SR_R512(NAME, S, X) __SR_R256(NAME, S, X); __SR_R256(NAME, S, X + 256);
#define __SR_FIXED_REDUCE(NAME, GROUP_SIZE, S) __SR_R##GROUP_SIZE(NAME, S, 0)

#define SR_DEF_SHARED_BUFFER(TYPE, NAME, GROUP_SIZE) groupshared TYPE s##NAME[GROUP_SIZE]
#define SR_REDUCE(TYPE, NAME, GROUP_SIZE, GROUP_IDX, IDX_BEGIN, IDX_END, LOAD, REDUCE) [unroll] do { \
    uint __sr_s = IDX_BEGIN, __sr_t = IDX_END, __sr__group_idx = GROUP_IDX; \
    [loop] for (; __sr_s + GROUP_SIZE <= __sr_t; __sr_s += GROUP_SIZE) { \
        GroupMemoryBarrierWithGroupSync(); \
        s##NAME[__sr__group_idx] = LOAD(__sr_s + __sr__group_idx); \
        GroupMemoryBarrierWithGroupSync(); \
        __SR_FIXED_REDUCE(NAME, GROUP_SIZE, REDUCE); \
    } \
    if (__sr_s < __sr_t) { \
        GroupMemoryBarrierWithGroupSync(); \
        if (__sr_s + __sr__group_idx < __sr_t) \
            s##NAME[__sr__group_idx] = LOAD(__sr_s + __sr__group_idx); \
        GroupMemoryBarrierWithGroupSync(); \
        for (uint __sr_i = 0, __sr_n = __sr_t - __sr_s; __sr_i < __sr_n; ++__sr_i) \
            REDUCE(s##NAME[__sr_i]); \
    } \
} while(false)