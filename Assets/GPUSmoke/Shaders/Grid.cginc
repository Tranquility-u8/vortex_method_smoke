#ifndef GRID_CGINC
#define GRID_CGINC

#define __GRID_BOUND_MIN(PREFIX) float3(u##PREFIX##BoundMin)
#define __GRID_SIZE(PREFIX) int3(u##PREFIX##GridSize)
#define __GRID_USIZE(PREFIX) uint3(u##PREFIX##GridSize)
#define __GRID_CELL_SIZE(PREFIX) float(u##PREFIX##CellSize)

// Define Uniforms
#define _GRID_DEF_UNIFORM(PREFIX) float u##PREFIX##CellSize; uint3 u##PREFIX##GridSize; float3 u##PREFIX##BoundMin;

// Cell Converts
#define __GRID_CELL_INT(PREFIX, CELL) int3(CELL)
#define __GRID_CELL_INT_HIGH(PREFIX, CELL) (int3(CELL) + 1)
#define __GRID_CELL_INT_FLOOR(PREFIX, CELL) int3(floor(CELL))
#define __GRID_CELL_INT_FLOOR_HIGH(PREFIX, CELL) (int3(floor(CELL)) + 1)
#define __GRID_CELL_INT_CLAMP(PREFIX, CELL) clamp(__GRID_CELL_INT(PREFIX, CELL), int3(0, 0, 0), __GRID_SIZE(PREFIX) - 1)
#define __GRID_CELL_FLT(PREFIX, CELL) float3(CELL)
#define __GRID_CELL_FLT_CENTER(PREFIX, CELL) (float3(__GRID_CELL_INT(PREFIX, CELL)) + 0.5)
#define __GRID_CELL_FLT_FLOOR_CENTER(PREFIX, CELL) (float3(__GRID_CELL_INT_FLOOR(PREFIX, CELL)) + 0.5)
#define __GRID_CELL_FLT_CLAMP(PREFIX, CELL) clamp(_GRID_CELL_FLT(PREFIX, CELL), float3(0, 0, 0), float3(__GRID_SIZE(PREFIX)))

// Units
#define _GRID_UNIT_UVW2WORLD(PREFIX) ((float3(__GRID_SIZE(PREFIX)) * __GRID_CELL_SIZE(PREFIX)))
#define _GRID_UNIT_WORLD2UVW(PREFIX) (1.0 / _GRID_UNIT_UVW2WORLD(PREFIX))
#define _GRID_UNIT_CELL2WORLD(PREFIX) (__GRID_CELL_SIZE(PREFIX))
#define _GRID_UNIT_WORLD2CELL(PREFIX) (1.0 / __GRID_CELL_SIZE(PREFIX))
#define _GRID_UNIT_CELL2UVW(PREFIX) (_GRID_UNIT_CELL2WORLD(PREFIX) * _GRID_UNIT_WORLD2UVW(PREFIX))
#define _GRID_UNIT_UVW2CELL(PREFIX) (_GRID_UNIT_UVW2WORLD(PREFIX) * _GRID_UNIT_WORLD2CELL(PREFIX))

// Cell <-> World <-> UVW
#define _GRID_CELL(PREFIX, CELL_F, CELL) __GRID_CELL_##CELL_F(PREFIX, CELL)
#define _GRID_CELL2WORLD(PREFIX, CELL_F, CELL) float3(float3(_GRID_CELL(PREFIX, CELL_F, CELL)) * __GRID_CELL_SIZE(PREFIX) + __GRID_BOUND_MIN(PREFIX))
#define _GRID_WORLD2CELL(PREFIX, CELL_F, WORLD) _GRID_CELL(PREFIX, CELL_F, (float3(WORLD) - __GRID_BOUND_MIN(PREFIX)) / __GRID_CELL_SIZE(PREFIX))
#define _GRID_CELL2UVW(PREFIX, CELL_F, CELL) float3(float3(_GRID_CELL(PREFIX, CELL_F, CELL)) / float3(__GRID_SIZE(PREFIX)))
#define _GRID_UVW2CELL(PREFIX, CELL_F, UVW) _GRID_CELL(PREFIX, CELL_F, float3(UVW) * float3(__GRID_SIZE(PREFIX)))
#define _GRID_UVW2WORLD(PREFIX, UVW) (float3(UVW) * _GRID_UNIT_UVW2WORLD(PREFIX) + __GRID_BOUND_MIN(PREFIX))
#define _GRID_WORLD2UVW(PREFIX, WORLD) ((float3(WORLD) - __GRID_BOUND_MIN(PREFIX)) * _GRID_UNIT_WORLD2UVW(PREFIX))
// In Bound Checks
#define _GRID_CELL_IN_GRID_INT(PREFIX, CELL) ( \
    0 <= (CELL).x && (CELL).x < __GRID_SIZE(PREFIX).x && \
    0 <= (CELL).y && (CELL).y < __GRID_SIZE(PREFIX).y && \
    0 <= (CELL).z && (CELL).z < __GRID_SIZE(PREFIX).z)
#define _GRID_CELL_IN_GRID_UINT(PREFIX, CELL) ( \
    (CELL).x < __GRID_USIZE(PREFIX).x && \
    (CELL).y < __GRID_USIZE(PREFIX).y && \
    (CELL).z < __GRID_USIZE(PREFIX).z)
// Cell (Int Clamp) <-> Cell ID
#define _GRID_CELL2ID(PREFIX, CELL) ((CELL).x + ((CELL).y + (CELL).z * __GRID_SIZE(PREFIX).y) * __GRID_SIZE(PREFIX).x)
// CellCount
#define _GRID_CELL_COUNT(PREFIX) (__GRID_SIZE(PREFIX).x * __GRID_SIZE(PREFIX).y * __GRID_SIZE(PREFIX).z)

// None Prefix
#define GRID_DEF_UNIFORM _GRID_DEF_UNIFORM()

#define GRID_UNIT_UVW2WORLD _GRID_UNIT_UVW2WORLD()
#define GRID_UNIT_WORLD2UVW _GRID_UNIT_WORLD2UVW()
#define GRID_UNIT_CELL2WORLD _GRID_UNIT_CELL2WORLD()
#define GRID_UNIT_WORLD2CELL _GRID_UNIT_WORLD2CELL()
#define GRID_UNIT_CELL2UVW _GRID_UNIT_CELL2UVW()
#define GRID_UNIT_UVW2CELL _GRID_UNIT_UVW2CELL()

#define GRID_CELL(CELL_F, CELL) _GRID_CELL(, CELL_F, CELL)
#define GRID_CELL2WORLD(CELL_F, CELL) _GRID_CELL2WORLD(, CELL_F, CELL)
#define GRID_WORLD2CELL(CELL_F, WORLD) _GRID_WORLD2CELL(, CELL_F, WORLD)
#define GRID_CELL2UVW(CELL_F, CELL) _GRID_CELL2UVW(, CELL_F, CELL)
#define GRID_UVW2CELL(CELL_F, UVW) _GRID_UVW2CELL(, CELL_F, UVW)
#define GRID_UVW2WORLD(UVW) _GRID_UVW2WORLD(, UVW)
#define GRID_WORLD2UVW(WORLD) _GRID_WORLD2UVW(, WORLD)
#define GRID_CELL_IN_GRID_INT(CELL) _GRID_CELL_IN_GRID_INT(, CELL)
#define GRID_CELL_IN_GRID_UINT(CELL) _GRID_CELL_IN_GRID_UINT(, CELL)
#define GRID_CELL2ID(CELL) _GRID_CELL2ID(, CELL)
#define GRID_CELL_COUNT _GRID_CELL_COUNT()

#endif