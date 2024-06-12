#ifndef MAT_COL_CGINC
#define MAT_COL_CGINC

#define FMAT3_ROW(M, X) (M[X])
#define FMAT4_ROW(M, X) (M[X])
#define FMAT3_COL(M, X) (float3(M[0][X], M[1][X], M[2][X]))
#define FMAT4_COL(M, X) (float4(M[0][X], M[1][X], M[2][X], M[3][X]))

#endif