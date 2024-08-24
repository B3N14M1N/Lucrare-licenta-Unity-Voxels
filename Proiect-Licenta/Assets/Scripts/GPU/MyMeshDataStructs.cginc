#ifndef MY_MESH_DATA_STRUCTS
#define MY_MESH_DATA_STRUCTS

static const float3 Vertices[8] =
{
    float3(0, 1, 0), //0
        float3(1, 1, 0), //1
        float3(1, 1, 1), //2
        float3(0, 1, 1), //3

        float3(0, 0, 0), //4
        float3(1, 0, 0), //5
        float3(1, 0, 1), //6
        float3(0, 0, 1) //7
};

static const float3 FaceCheck[6] =
{
    float3(0, 0, -1), //back 0
        float3(1, 0, 0), //right 1
        float3(0, 0, 1), //front 2
        float3(-1, 0, 0), //left 3
        float3(0, 1, 0), //top 4
        float3(0, -1, 0) //bottom 5
};

static const int FaceVerticeIndex[6][4] =
{
    { 4, 5, 1, 0 },
    { 5, 6, 2, 1 },
    { 6, 7, 3, 2 },
    { 7, 4, 0, 3 },
    { 0, 1, 2, 3 },
    { 7, 6, 5, 4 },
};

static const float2 verticeUVs[4] =
{
    float2(0, 0),
        float2(1, 0),
        float2(1, 1),
        float2(0, 1)
};

static const int faceIndices[6] =
{
    0, 3, 2, 0, 2, 1
};

#endif