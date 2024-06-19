/******************************************************************************
 * GPUSorting
 *
 * SPDX-License-Identifier: MIT
 * Copyright Thomas Smith 4/28/2024
 * https://github.com/b0nes164/GPUSorting
 *
 ******************************************************************************/
#pragma multi_compile __ KEY_UINT KEY_INT KEY_FLOAT
#pragma multi_compile __ PAYLOAD_UINT PAYLOAD_INT PAYLOAD_FLOAT
#pragma multi_compile __ SHOULD_ASCEND
#pragma multi_compile __ SORT_PAIRS

#pragma use_dxc
#pragma require wavebasic
#pragma require waveballot
#if defined(ENABLE_16_BIT)
#pragma require Native16Bit
#endif

#define KEYS_PER_THREAD     15U 
#define D_DIM               256U
#define COPY_DIM            256U
#define PART_SIZE           3840U
#define D_TOTAL_SMEM        4096U

#define MAX_DISPATCH_DIM    65535U  //The max value of any given dispatch dimension
#define RADIX               256U    //Number of digit bins
#define RADIX_MASK          255U    //Mask of digit bins
#define HALF_RADIX          128U    //For smaller waves where bit packing is necessary
#define HALF_MASK           127U    // '' 
#define RADIX_LOG           8U      //log2(RADIX)
#define RADIX_PASSES        4U      //(Key width) / RADIX_LOG

cbuffer cbGpuSorting : register(b0)
{
    uint e_numKeys;
    uint e_radixShift;
    uint e_threadBlocks;
    uint padding;
};

#if defined(KEY_UINT)
RWStructuredBuffer<uint> b_sort;
RWStructuredBuffer<uint> b_alt;
#elif defined(KEY_INT)
RWStructuredBuffer<int> b_sort;
RWStructuredBuffer<int> b_alt;
#elif defined(KEY_FLOAT)
RWStructuredBuffer<float> b_sort;
RWStructuredBuffer<float> b_alt;
#endif

#if defined(PAYLOAD_UINT)
RWStructuredBuffer<uint> b_sortPayload;
RWStructuredBuffer<uint> b_altPayload;
#elif defined(PAYLOAD_INT)
RWStructuredBuffer<int> b_sortPayload;
RWStructuredBuffer<int> b_altPayload;
#elif defined(PAYLOAD_FLOAT)
RWStructuredBuffer<float> b_sortPayload;
RWStructuredBuffer<float> b_altPayload;
#endif

groupshared uint g_d[D_TOTAL_SMEM]; //Shared memory for DigitBinningPass and DownSweep kernels

#pragma kernel Copy
[numthreads(COPY_DIM, 1, 1)]
void Copy(uint3 id : SV_DispatchThreadID) {
    if (id.x >= e_numKeys)
        return;

#if defined(KEY_UINT) || defined(KEY_INT) || defined(KEY_FLOAT)
    b_alt[id.x] = b_sort[id.x];
#endif
#if defined(SORT_PAIRS)
#if defined(PAYLOAD_UINT) || defined(PAYLOAD_INT) || defined(PAYLOAD_FLOAT)
    b_altPayload[id.x] = b_sortPayload[id.x];
#endif
#endif
}

struct KeyStruct
{
    uint k[KEYS_PER_THREAD];
};

struct OffsetStruct
{
#if defined(ENABLE_16_BIT)
    uint16_t o[KEYS_PER_THREAD];
#else
    uint o[KEYS_PER_THREAD];
#endif
};

struct DigitStruct
{
#if defined(ENABLE_16_BIT)
    uint16_t d[KEYS_PER_THREAD];
#else
    uint d[KEYS_PER_THREAD];
#endif
};

//*****************************************************************************
//HELPER FUNCTIONS
//*****************************************************************************
inline uint getWaveIndex(uint gtid)
{
    return gtid / WaveGetLaneCount();
}

//Radix Tricks by Michael Herf
//http://stereopsis.com/radix.html
inline uint FloatToUint(float f)
{
    uint mask = -((int) (asuint(f) >> 31)) | 0x80000000;
    return asuint(f) ^ mask;
}

inline float UintToFloat(uint u)
{
    uint mask = ((u >> 31) - 1) | 0x80000000;
    return asfloat(u ^ mask);
}

inline uint IntToUint(int i)
{
    return asuint(i ^ 0x80000000);
}

inline int UintToInt(uint u)
{
    return asint(u ^ 0x80000000);
}

inline uint getWaveCountPass()
{
    return D_DIM / WaveGetLaneCount();
}

inline uint ExtractDigit(uint key)
{
    return key >> e_radixShift & RADIX_MASK;
}

inline uint ExtractDigit(uint key, uint shift)
{
    return key >> shift & RADIX_MASK;
}

inline uint ExtractPackedIndex(uint key)
{
    return key >> (e_radixShift + 1) & HALF_MASK;
}

inline uint ExtractPackedShift(uint key)
{
    return (key >> e_radixShift & 1) ? 16 : 0;
}

inline uint ExtractPackedValue(uint packed, uint key)
{
    return packed >> ExtractPackedShift(key) & 0xffff;
}

inline uint SubPartSizeWGE16()
{
    return KEYS_PER_THREAD * WaveGetLaneCount();
}

inline uint SharedOffsetWGE16(uint gtid)
{
    return WaveGetLaneIndex() + getWaveIndex(gtid) * SubPartSizeWGE16();
}

inline uint SubPartSizeWLT16(uint _serialIterations)
{
    return KEYS_PER_THREAD * WaveGetLaneCount() * _serialIterations;
}

inline uint SharedOffsetWLT16(uint gtid, uint _serialIterations)
{
    return WaveGetLaneIndex() +
        (getWaveIndex(gtid) / _serialIterations * SubPartSizeWLT16(_serialIterations)) +
        (getWaveIndex(gtid) % _serialIterations * WaveGetLaneCount());
}

inline uint DeviceOffsetWGE16(uint gtid, uint partIndex)
{
    return SharedOffsetWGE16(gtid) + partIndex * PART_SIZE;
}

inline uint DeviceOffsetWLT16(uint gtid, uint partIndex, uint serialIterations)
{
    return SharedOffsetWLT16(gtid, serialIterations) + partIndex * PART_SIZE;
}

inline uint GlobalHistOffset()
{
    return e_radixShift << 5;
}

inline uint WaveHistsSizeWGE16()
{
    return D_DIM / WaveGetLaneCount() * RADIX;
}

inline uint WaveHistsSizeWLT16()
{
    return D_TOTAL_SMEM;
}

//*****************************************************************************
//FUNCTIONS COMMON TO THE DOWNSWEEP / DIGIT BINNING PASS
//*****************************************************************************
//If the size of  a wave is too small, we do not have enough space in
//shared memory to assign a histogram to each wave, so instead,
//some operations are peformed serially.
inline uint SerialIterations()
{
    return (D_DIM / WaveGetLaneCount() + 31) >> 5;
}

inline void ClearWaveHists(uint gtid)
{
    const uint histsEnd = WaveGetLaneCount() >= 16 ?
        WaveHistsSizeWGE16() : WaveHistsSizeWLT16();
    for (uint i = gtid; i < histsEnd; i += D_DIM)
        g_d[i] = 0;
}

inline void LoadKey(inout uint key, uint index)
{
#if defined(KEY_UINT)
    key = b_sort[index];
#elif defined(KEY_INT)
    key = UintToInt(b_sort[index]);
#elif defined(KEY_FLOAT)
    key = FloatToUint(b_sort[index]);
#endif
}

inline void LoadDummyKey(inout uint key)
{
    key = 0xffffffff;
}

inline KeyStruct LoadKeysWGE16(uint gtid, uint partIndex)
{
    KeyStruct keys;
    [unroll]
    for (uint i = 0, t = DeviceOffsetWGE16(gtid, partIndex);
        i < KEYS_PER_THREAD;
        ++i, t += WaveGetLaneCount())
    {
        LoadKey(keys.k[i], t);
    }
    return keys;
}

inline KeyStruct LoadKeysWLT16(uint gtid, uint partIndex, uint serialIterations)
{
    KeyStruct keys;
    [unroll]
    for (uint i = 0, t = DeviceOffsetWLT16(gtid, partIndex, serialIterations);
        i < KEYS_PER_THREAD;
        ++i, t += WaveGetLaneCount() * serialIterations)
    {
        LoadKey(keys.k[i], t);
    }
    return keys;
}

inline KeyStruct LoadKeysPartialWGE16(uint gtid, uint partIndex)
{
    KeyStruct keys;
    [unroll]
    for (uint i = 0, t = DeviceOffsetWGE16(gtid, partIndex);
                 i < KEYS_PER_THREAD;
                 ++i, t += WaveGetLaneCount())
    {
        if (t < e_numKeys)
            LoadKey(keys.k[i], t);
        else
            LoadDummyKey(keys.k[i]);
    }
    return keys;
}

inline KeyStruct LoadKeysPartialWLT16(uint gtid, uint partIndex, uint serialIterations)
{
    KeyStruct keys;
    [unroll]
    for (uint i = 0, t = DeviceOffsetWLT16(gtid, partIndex, serialIterations);
        i < KEYS_PER_THREAD;
        ++i, t += WaveGetLaneCount() * serialIterations)
    {
        if (t < e_numKeys)
            LoadKey(keys.k[i], t);
        else
            LoadDummyKey(keys.k[i]);
    }
    return keys;
}

inline uint WaveFlagsWGE16()
{
    return (WaveGetLaneCount() & 31) ?
        (1U << WaveGetLaneCount()) - 1 : 0xffffffff;
}

inline uint WaveFlagsWLT16()
{
    return (1U << WaveGetLaneCount()) - 1;;
}

inline void WarpLevelMultiSplitWGE16(uint key, uint waveParts, inout uint4 waveFlags)
{
    [unroll]
    for (uint k = 0; k < RADIX_LOG; ++k)
    {
        const bool t = key >> (k + e_radixShift) & 1;
        const uint4 ballot = WaveActiveBallot(t);
        for (uint wavePart = 0; wavePart < waveParts; ++wavePart)
            waveFlags[wavePart] &= (t ? 0 : 0xffffffff) ^ ballot[wavePart];
    }
}

inline void WarpLevelMultiSplitWLT16(uint key, inout uint waveFlags)
{
    [unroll]
    for (uint k = 0; k < RADIX_LOG; ++k)
    {
        const bool t = key >> (k + e_radixShift) & 1;
        waveFlags &= (t ? 0 : 0xffffffff) ^ (uint) WaveActiveBallot(t);
    }
}

inline void CountPeerBits(
    inout uint peerBits,
    inout uint totalBits,
    uint4 waveFlags,
    uint waveParts)
{
    for (uint wavePart = 0; wavePart < waveParts; ++wavePart)
    {
        if (WaveGetLaneIndex() >= wavePart * 32)
        {
            const uint ltMask = WaveGetLaneIndex() >= (wavePart + 1) * 32 ?
                0xffffffff : (1U << (WaveGetLaneIndex() & 31)) - 1;
            peerBits += countbits(waveFlags[wavePart] & ltMask);
        }
        totalBits += countbits(waveFlags[wavePart]);
    }
}

inline uint CountPeerBitsWLT16(
    uint waveFlags,
    uint ltMask)
{
    return countbits(waveFlags & ltMask);
}

inline uint FindLowestRankPeer(
    uint4 waveFlags,
    uint waveParts)
{
    uint lowestRankPeer = 0;
    for (uint wavePart = 0; wavePart < waveParts; ++wavePart)
    {
        uint fbl = firstbitlow(waveFlags[wavePart]);
        if (fbl == 0xffffffff)
            lowestRankPeer += 32;
        else
            return lowestRankPeer + fbl;
    }
    return 0; //will never happen
}

inline OffsetStruct RankKeysWGE16(uint gtid, KeyStruct keys)
{
    OffsetStruct offsets;
    const uint waveParts = (WaveGetLaneCount() + 31) / 32;
    [unroll]
    for (uint i = 0; i < KEYS_PER_THREAD; ++i)
    {
        uint4 waveFlags = WaveFlagsWGE16();
        WarpLevelMultiSplitWGE16(keys.k[i], waveParts, waveFlags);
        
        const uint index = ExtractDigit(keys.k[i]) + (getWaveIndex(gtid.x) * RADIX);
        const uint lowestRankPeer = FindLowestRankPeer(waveFlags, waveParts);
        
        uint peerBits = 0;
        uint totalBits = 0;
        CountPeerBits(peerBits, totalBits, waveFlags, waveParts);
        
        uint preIncrementVal;
        if (peerBits == 0)
            InterlockedAdd(g_d[index], totalBits, preIncrementVal);
        offsets.o[i] = WaveReadLaneAt(preIncrementVal, lowestRankPeer) + peerBits;
    }
    
    return offsets;
}

inline OffsetStruct RankKeysWLT16(uint gtid, KeyStruct keys, uint serialIterations)
{
    OffsetStruct offsets;
    const uint ltMask = (1U << WaveGetLaneIndex()) - 1;
    
    [unroll]
    for (uint i = 0; i < KEYS_PER_THREAD; ++i)
    {
        uint waveFlags = WaveFlagsWLT16();
        WarpLevelMultiSplitWLT16(keys.k[i], waveFlags);
        
        const uint index = ExtractPackedIndex(keys.k[i]) +
                (getWaveIndex(gtid.x) / serialIterations * HALF_RADIX);
        
        const uint peerBits = CountPeerBitsWLT16(waveFlags, ltMask);
        for (uint k = 0; k < serialIterations; ++k)
        {
            if (getWaveIndex(gtid.x) % serialIterations == k)
                offsets.o[i] = ExtractPackedValue(g_d[index], keys.k[i]) + peerBits;
            
            GroupMemoryBarrierWithGroupSync();
            if (getWaveIndex(gtid.x) % serialIterations == k && peerBits == 0)
            {
                InterlockedAdd(g_d[index],
                    countbits(waveFlags) << ExtractPackedShift(keys.k[i]));
            }
            GroupMemoryBarrierWithGroupSync();
        }
    }
    
    return offsets;
}

inline uint WaveHistInclusiveScanCircularShiftWGE16(uint gtid)
{
    uint histReduction = g_d[gtid];
    for (uint i = gtid + RADIX; i < WaveHistsSizeWGE16(); i += RADIX)
    {
        histReduction += g_d[i];
        g_d[i] = histReduction - g_d[i];
    }
    return histReduction;
}

inline uint WaveHistInclusiveScanCircularShiftWLT16(uint gtid)
{
    uint histReduction = g_d[gtid];
    for (uint i = gtid + HALF_RADIX; i < WaveHistsSizeWLT16(); i += HALF_RADIX)
    {
        histReduction += g_d[i];
        g_d[i] = histReduction - g_d[i];
    }
    return histReduction;
}

inline void WaveHistReductionExclusiveScanWGE16(uint gtid, uint histReduction)
{
    if (gtid < RADIX)
    {
        const uint laneMask = WaveGetLaneCount() - 1;
        g_d[((WaveGetLaneIndex() + 1) & laneMask) + (gtid & ~laneMask)] = histReduction;
    }
    GroupMemoryBarrierWithGroupSync();
                
    if (gtid < RADIX / WaveGetLaneCount())
    {
        g_d[gtid * WaveGetLaneCount()] =
            WavePrefixSum(g_d[gtid * WaveGetLaneCount()]);
    }
    GroupMemoryBarrierWithGroupSync();
                
    if (gtid < RADIX && WaveGetLaneIndex())
        g_d[gtid] += WaveReadLaneAt(g_d[gtid - 1], 1);
}

//inclusive/exclusive prefix sum up the histograms,
//use a blelloch scan for in place packed exclusive
inline void WaveHistReductionExclusiveScanWLT16(uint gtid)
{
    uint shift = 1;
    for (uint j = RADIX >> 2; j > 0; j >>= 1)
    {
        GroupMemoryBarrierWithGroupSync();
        if (gtid < j)
        {
            g_d[((((gtid << 1) + 2) << shift) - 1) >> 1] +=
                g_d[((((gtid << 1) + 1) << shift) - 1) >> 1] & 0xffff0000;
        }
        shift++;
    }
    GroupMemoryBarrierWithGroupSync();
                
    if (gtid == 0)
        g_d[HALF_RADIX - 1] &= 0xffff;
                
    for (uint j = 1; j < RADIX >> 1; j <<= 1)
    {
        --shift;
        GroupMemoryBarrierWithGroupSync();
        if (gtid < j)
        {
            const uint t = ((((gtid << 1) + 1) << shift) - 1) >> 1;
            const uint t2 = ((((gtid << 1) + 2) << shift) - 1) >> 1;
            const uint t3 = g_d[t];
            g_d[t] = (g_d[t] & 0xffff) | (g_d[t2] & 0xffff0000);
            g_d[t2] += t3 & 0xffff0000;
        }
    }

    GroupMemoryBarrierWithGroupSync();
    if (gtid < HALF_RADIX)
    {
        const uint t = g_d[gtid];
        g_d[gtid] = (t >> 16) + (t << 16) + (t & 0xffff0000);
    }
}

inline void UpdateOffsetsWGE16(uint gtid, inout OffsetStruct offsets, KeyStruct keys)
{
    if (gtid >= WaveGetLaneCount())
    {
        const uint t = getWaveIndex(gtid) * RADIX;
        [unroll]
        for (uint i = 0; i < KEYS_PER_THREAD; ++i)
        {
            const uint t2 = ExtractDigit(keys.k[i]);
            offsets.o[i] += g_d[t2 + t] + g_d[t2];
        }
    }
    else
    {
        [unroll]
        for (uint i = 0; i < KEYS_PER_THREAD; ++i)
            offsets.o[i] += g_d[ExtractDigit(keys.k[i])];
    }
}

inline void UpdateOffsetsWLT16(
    uint gtid,
    uint serialIterations,
    inout OffsetStruct offsets,
    KeyStruct keys)
{
    if (gtid >= WaveGetLaneCount() * serialIterations)
    {
        const uint t = getWaveIndex(gtid) / serialIterations * HALF_RADIX;
        [unroll]
        for (uint i = 0; i < KEYS_PER_THREAD; ++i)
        {
            const uint t2 = ExtractPackedIndex(keys.k[i]);
            offsets.o[i] += ExtractPackedValue(g_d[t2 + t] + g_d[t2], keys.k[i]);
        }
    }
    else
    {
        [unroll]
        for (uint i = 0; i < KEYS_PER_THREAD; ++i)
            offsets.o[i] += ExtractPackedValue(g_d[ExtractPackedIndex(keys.k[i])], keys.k[i]);
    }
}

inline void ScatterKeysShared(OffsetStruct offsets, KeyStruct keys)
{
    [unroll]
    for (uint i = 0; i < KEYS_PER_THREAD; ++i)
        g_d[offsets.o[i]] = keys.k[i];
}

inline uint DescendingIndex(uint deviceIndex)
{
    return e_numKeys - deviceIndex - 1;
}

inline void WriteKey(uint deviceIndex, uint groupSharedIndex)
{
#if defined(KEY_UINT)
    b_alt[deviceIndex] = g_d[groupSharedIndex];
#elif defined(KEY_INT)
    b_alt[deviceIndex] = UintToInt(g_d[groupSharedIndex]);
#elif defined(KEY_FLOAT)
    b_alt[deviceIndex] = UintToFloat(g_d[groupSharedIndex]);
#endif
}

inline void LoadPayload(inout uint payload, uint deviceIndex)
{
#if defined(PAYLOAD_UINT)
    payload = b_sortPayload[deviceIndex];
#elif defined(PAYLOAD_INT) || defined(PAYLOAD_FLOAT)
    payload = asuint(b_sortPayload[deviceIndex]);
#endif
}

inline void ScatterPayloadsShared(OffsetStruct offsets, KeyStruct payloads)
{
    ScatterKeysShared(offsets, payloads);
}

inline void WritePayload(uint deviceIndex, uint groupSharedIndex)
{
#if defined(PAYLOAD_UINT)
    b_altPayload[deviceIndex] = g_d[groupSharedIndex];
#elif defined(PAYLOAD_INT)
    b_altPayload[deviceIndex] = asint(g_d[groupSharedIndex]);
#elif defined(PAYLOAD_FLOAT)
    b_altPayload[deviceIndex] = asfloat(g_d[groupSharedIndex]);
#endif
}

//*****************************************************************************
//SCATTERING: FULL PARTITIONS
//*****************************************************************************
//KEYS ONLY
inline void ScatterKeysOnlyDeviceAscending(uint gtid)
{
    for (uint i = gtid; i < PART_SIZE; i += D_DIM)
        WriteKey(g_d[ExtractDigit(g_d[i]) + PART_SIZE] + i, i);
}

inline void ScatterKeysOnlyDeviceDescending(uint gtid)
{
    if (e_radixShift == 24)
    {
        for (uint i = gtid; i < PART_SIZE; i += D_DIM)
            WriteKey(DescendingIndex(g_d[ExtractDigit(g_d[i]) + PART_SIZE] + i), i);
    }
    else
    {
        ScatterKeysOnlyDeviceAscending(gtid);
    }
}

inline void ScatterKeysOnlyDevice(uint gtid)
{
#if defined(SHOULD_ASCEND)
    ScatterKeysOnlyDeviceAscending(gtid);
#else
    ScatterKeysOnlyDeviceDescending(gtid);
#endif
}

//KEY VALUE PAIRS
inline void ScatterPairsKeyPhaseAscending(
    uint gtid,
    inout DigitStruct digits)
{
    [unroll]
    for (uint i = 0, t = gtid; i < KEYS_PER_THREAD; ++i, t += D_DIM)
    {
        digits.d[i] = ExtractDigit(g_d[t]);
        WriteKey(g_d[digits.d[i] + PART_SIZE] + t, t);
    }
}

inline void ScatterPairsKeyPhaseDescending(
    uint gtid,
    inout DigitStruct digits)
{
    if (e_radixShift == 24)
    {
        [unroll]
        for (uint i = 0, t = gtid; i < KEYS_PER_THREAD; ++i, t += D_DIM)
        {
            digits.d[i] = ExtractDigit(g_d[t]);
            WriteKey(DescendingIndex(g_d[digits.d[i] + PART_SIZE] + t), t);
        }
    }
    else
    {
        ScatterPairsKeyPhaseAscending(gtid, digits);
    }
}

inline void LoadPayloadsWGE16(
    uint gtid,
    uint partIndex,
    inout KeyStruct payloads)
{
    [unroll]
    for (uint i = 0, t = DeviceOffsetWGE16(gtid, partIndex);
        i < KEYS_PER_THREAD;
        ++i, t += WaveGetLaneCount())
    {
        LoadPayload(payloads.k[i], t);
    }
}

inline void LoadPayloadsWLT16(
    uint gtid,
    uint partIndex,
    uint serialIterations,
    inout KeyStruct payloads)
{
    [unroll]
    for (uint i = 0, t = DeviceOffsetWLT16(gtid, partIndex, serialIterations);
        i < KEYS_PER_THREAD;
        ++i, t += WaveGetLaneCount() * serialIterations)
    {
        LoadPayload(payloads.k[i], t);
    }
}

inline void ScatterPayloadsAscending(uint gtid, DigitStruct digits)
{
    [unroll]
    for (uint i = 0, t = gtid; i < KEYS_PER_THREAD; ++i, t += D_DIM)
        WritePayload(g_d[digits.d[i] + PART_SIZE] + t, t);
}

inline void ScatterPayloadsDescending(uint gtid, DigitStruct digits)
{
    if (e_radixShift == 24)
    {
        [unroll]
        for (uint i = 0, t = gtid; i < KEYS_PER_THREAD; ++i, t += D_DIM)
            WritePayload(DescendingIndex(g_d[digits.d[i] + PART_SIZE] + t), t);
    }
    else
    {
        ScatterPayloadsAscending(gtid, digits);
    }
}

inline void ScatterPairsDevice(
    uint gtid,
    uint partIndex,
    OffsetStruct offsets)
{
    DigitStruct digits;
#if defined(SHOULD_ASCEND)
    ScatterPairsKeyPhaseAscending(gtid, digits);
#else
    ScatterPairsKeyPhaseDescending(gtid, digits);
#endif
    GroupMemoryBarrierWithGroupSync();
    
    KeyStruct payloads;
    if (WaveGetLaneCount() >= 16)
        LoadPayloadsWGE16(gtid, partIndex, payloads);
    else
        LoadPayloadsWLT16(gtid, partIndex, SerialIterations(), payloads);
    ScatterPayloadsShared(offsets, payloads);
    GroupMemoryBarrierWithGroupSync();
    
#if defined(SHOULD_ASCEND)
    ScatterPayloadsAscending(gtid, digits);
#else
    ScatterPayloadsDescending(gtid, digits);
#endif
}

inline void ScatterDevice(
    uint gtid,
    uint partIndex,
    OffsetStruct offsets)
{
#if defined(SORT_PAIRS)
    ScatterPairsDevice(
        gtid,
        partIndex,
        offsets);
#else
    ScatterKeysOnlyDevice(gtid);
#endif
}

//*****************************************************************************
//SCATTERING: PARTIAL PARTITIONS
//*****************************************************************************
//KEYS ONLY
inline void ScatterKeysOnlyDevicePartialAscending(uint gtid, uint finalPartSize)
{
    for (uint i = gtid; i < PART_SIZE; i += D_DIM)
    {
        if (i < finalPartSize)
            WriteKey(g_d[ExtractDigit(g_d[i]) + PART_SIZE] + i, i);
    }
}

inline void ScatterKeysOnlyDevicePartialDescending(uint gtid, uint finalPartSize)
{
    if (e_radixShift == 24)
    {
        for (uint i = gtid; i < PART_SIZE; i += D_DIM)
        {
            if (i < finalPartSize)
                WriteKey(DescendingIndex(g_d[ExtractDigit(g_d[i]) + PART_SIZE] + i), i);
        }
    }
    else
    {
        ScatterKeysOnlyDevicePartialAscending(gtid, finalPartSize);
    }
}

inline void ScatterKeysOnlyDevicePartial(uint gtid, uint partIndex)
{
    const uint finalPartSize = e_numKeys - partIndex * PART_SIZE;
#if defined(SHOULD_ASCEND)
    ScatterKeysOnlyDevicePartialAscending(gtid, finalPartSize);
#else
    ScatterKeysOnlyDevicePartialDescending(gtid, finalPartSize);
#endif
}

//KEY VALUE PAIRS
inline void ScatterPairsKeyPhaseAscendingPartial(
    uint gtid,
    uint finalPartSize,
    inout DigitStruct digits)
{
    [unroll]
    for (uint i = 0, t = gtid; i < KEYS_PER_THREAD; ++i, t += D_DIM)
    {
        if (t < finalPartSize)
        {
            digits.d[i] = ExtractDigit(g_d[t]);
            WriteKey(g_d[digits.d[i] + PART_SIZE] + t, t);
        }
    }
}

inline void ScatterPairsKeyPhaseDescendingPartial(
    uint gtid,
    uint finalPartSize,
    inout DigitStruct digits)
{
    if (e_radixShift == 24)
    {
        [unroll]
        for (uint i = 0, t = gtid; i < KEYS_PER_THREAD; ++i, t += D_DIM)
        {
            if (t < finalPartSize)
            {
                digits.d[i] = ExtractDigit(g_d[t]);
                WriteKey(DescendingIndex(g_d[digits.d[i] + PART_SIZE] + t), t);
            }
        }
    }
    else
    {
        ScatterPairsKeyPhaseAscendingPartial(gtid, finalPartSize, digits);
    }
}

inline void LoadPayloadsPartialWGE16(
    uint gtid,
    uint partIndex,
    inout KeyStruct payloads)
{
    [unroll]
    for (uint i = 0, t = DeviceOffsetWGE16(gtid, partIndex);
        i < KEYS_PER_THREAD;
        ++i, t += WaveGetLaneCount())
    {
        if (t < e_numKeys)
            LoadPayload(payloads.k[i], t);
    }
}

inline void LoadPayloadsPartialWLT16(
    uint gtid,
    uint partIndex,
    uint serialIterations,
    inout KeyStruct payloads)
{
    [unroll]
    for (uint i = 0, t = DeviceOffsetWLT16(gtid, partIndex, serialIterations);
        i < KEYS_PER_THREAD;
        ++i, t += WaveGetLaneCount() * serialIterations)
    {
        if (t < e_numKeys)
            LoadPayload(payloads.k[i], t);
    }
}

inline void ScatterPayloadsAscendingPartial(
    uint gtid,
    uint finalPartSize,
    DigitStruct digits)
{
    [unroll]
    for (uint i = 0, t = gtid; i < KEYS_PER_THREAD; ++i, t += D_DIM)
    {
        if (t < finalPartSize)
            WritePayload(g_d[digits.d[i] + PART_SIZE] + t, t);
    }
}

inline void ScatterPayloadsDescendingPartial(
    uint gtid,
    uint finalPartSize,
    DigitStruct digits)
{
    if (e_radixShift == 24)
    {
        [unroll]
        for (uint i = 0, t = gtid; i < KEYS_PER_THREAD; ++i, t += D_DIM)
        {
            if (t < finalPartSize)
                WritePayload(DescendingIndex(g_d[digits.d[i] + PART_SIZE] + t), t);
        }
    }
    else
    {
        ScatterPayloadsAscendingPartial(gtid, finalPartSize, digits);
    }
}

inline void ScatterPairsDevicePartial(
    uint gtid,
    uint partIndex,
    OffsetStruct offsets)
{
    DigitStruct digits;
    const uint finalPartSize = e_numKeys - partIndex * PART_SIZE;
#if defined(SHOULD_ASCEND)
    ScatterPairsKeyPhaseAscendingPartial(gtid, finalPartSize, digits);
#else
    ScatterPairsKeyPhaseDescendingPartial(gtid, finalPartSize, digits);
#endif
    GroupMemoryBarrierWithGroupSync();
    
    KeyStruct payloads;
    if (WaveGetLaneCount() >= 16)
        LoadPayloadsPartialWGE16(gtid, partIndex, payloads);
    else
        LoadPayloadsPartialWLT16(gtid, partIndex, SerialIterations(), payloads);
    ScatterPayloadsShared(offsets, payloads);
    GroupMemoryBarrierWithGroupSync();
    
#if defined(SHOULD_ASCEND)
    ScatterPayloadsAscendingPartial(gtid, finalPartSize, digits);
#else
    ScatterPayloadsDescendingPartial(gtid, finalPartSize, digits);
#endif
}

inline void ScatterDevicePartial(
    uint gtid,
    uint partIndex,
    OffsetStruct offsets)
{
#if defined(SORT_PAIRS)
    ScatterPairsDevicePartial(
        gtid,
        partIndex,
        offsets);
#else
    ScatterKeysOnlyDevicePartial(gtid, partIndex);
#endif
}