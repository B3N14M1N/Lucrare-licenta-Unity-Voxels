#ifndef MY_DATA_STRUCTS
#define MY_DATA_STRUCTS

/*
 * Voxel
 */

#define none  (uint)0
#define solid (uint)1
#define liquid (uint)2

#define air (uint)0
#define dirt (uint)1
#define sand (uint)2

#define ALL_MASK (uint) 0xFFFFFFFF

#define VOXEL_TYPE_MASK (uint)0xFF
#define EXCLUDE_VOXEL_TYPE_MASK (uint)0xFFFFFF00

#define PHYSICS_TYPE_MASK (uint)0xFF00
#define EXCLUDE_PHYSICS_TYPE_MASK (uint)0xFFFF00FF

struct Voxel
{
    int ID;
};


static void SetPhysicsType(inout Voxel voxel, uint type)
{
    voxel.ID = (voxel.ID & EXCLUDE_PHYSICS_TYPE_MASK) + (type << 8);
}

static void SetVoxelType(inout Voxel voxel, uint type)
{
    voxel.ID = (voxel.ID & EXCLUDE_VOXEL_TYPE_MASK) + type;
}

static uint GetPhysicsType(Voxel voxel)
{
    return (voxel.ID & PHYSICS_TYPE_MASK) >> 8;
}

static uint GetVoxelType(Voxel voxel)
{
    return voxel.ID & VOXEL_TYPE_MASK;
}
////////////////////////////////////////////////////////////////////////


/*
 * Height map
 */

#define ALL_MASK (uint)0xFFFFFFFF

#define SOLID_MASK (uint)0xFF
#define EXCLUDE_SOLID_MASK (uint)0xFFFFFF00

#define LIQUID_BOTTOM_MASK (uint)0xFF00
#define EXCLUDE_LIQUID_BOTTOM_MASK (uint)0xFFFF00FF

#define LIQUID_TOP_MASK (uint)0xFF0000
#define EXCLUDE_LIQUID_TOP_MASK (uint)0xFF00FFFF

struct HeightMap
{
    uint data;
};

static void SetSolid(inout HeightMap map, uint height)
{
    map.data = (map.data & EXCLUDE_SOLID_MASK) + height;
}

static void SetLiquidTop(inout HeightMap map, uint height)
{
    map.data = (map.data & EXCLUDE_LIQUID_TOP_MASK) + (height << 16);
}

static void SetLiquidBottom(inout HeightMap map, uint height)
{
    map.data = (map.data & EXCLUDE_LIQUID_BOTTOM_MASK) + (height << 8);
}

static uint GetSolid(HeightMap map)
{
    return map.data & SOLID_MASK;
}

static uint GetLiquidTop(HeightMap map)
{
    return (map.data & LIQUID_TOP_MASK) >> 16;
}

static uint GetLiquidBottom(HeightMap map)
{
    return (map.data & LIQUID_BOTTOM_MASK) >> 8;
}

/*
 *  Noise Parameters
 */

struct NoiseParameters
{
    float scale;
    float frequency;
    float lacunarity;
    float persistence;
    int octaves;
};
#endif