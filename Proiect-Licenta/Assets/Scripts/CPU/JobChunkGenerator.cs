using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class JobChunkGenerator
{
    public NativeArray<Voxel> voxels;
    public NativeArray<HeightMap> heightMaps;
    public MeshDataStruct meshData;
    public Vector3 chunkPos;
    private NativeArray<NoiseParameters> noiseParameters;
    private NativeArray<Vector2Int> octaveOffsets;
    private float globalScale;

    public bool GenerationStarted { get; private set; }
    public bool DataScheduled { get; private set; }
    public bool MeshScheduled { get; private set; }
    public bool DataGenerated { get; private set; }
    public bool MeshGenerated { get; private set; }

    private ChunkDataJob dataJob;
    public JobHandle dataHandle;

    private ChunkMeshJob meshJob;
    public JobHandle meshHandle;

    public JobChunkGenerator(Vector3 chunkPos, NoiseParameters[] noiseParameters, Vector2Int[] octaveOffsets, float globalScale)
    {
        this.chunkPos = chunkPos;
        this.globalScale = globalScale;
        GenerationStarted = false;
        DataGenerated = false;
        MeshGenerated = false;
        this.noiseParameters = new NativeArray<NoiseParameters>(noiseParameters, Allocator.Persistent);
        this.octaveOffsets = new NativeArray<Vector2Int>(octaveOffsets, Allocator.Persistent);
        ScheduleDataGeneration();
    }

    public bool IsComplete => GenerationStarted && DataGenerated && MeshGenerated;

    public void ScheduleDataGeneration()
    {
        if (!GenerationStarted && !DataScheduled && !DataGenerated)
        {
            voxels = new NativeArray<Voxel>((WorldSettings.ChunkWidth + 2) * WorldSettings.ChunkHeight * (WorldSettings.ChunkWidth + 2), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            heightMaps = new NativeArray<HeightMap>((WorldSettings.ChunkWidth + 2) * (WorldSettings.ChunkWidth + 2), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            dataJob = new ChunkDataJob()
            {
                stress = WorldSettings.StressTest,
                chunkWidth = WorldSettings.ChunkWidth,
                chunkHeight = WorldSettings.ChunkHeight,
                voxels = this.voxels,
                heightMaps = this.heightMaps,
                chunkPos = this.chunkPos,
                noiseParameters = this.noiseParameters,
                octaveOffsets = this.octaveOffsets,
                globalScale = this.globalScale,
            };
            GenerationStarted = true;
            DataScheduled = true;
            dataHandle = dataJob.Schedule();
        }
    }
    public bool CompleteDataGeneration()
    {
        if (GenerationStarted && DataScheduled && !DataGenerated && dataHandle.IsCompleted)
        {
            dataHandle.Complete();
            DataScheduled = false;
            DataGenerated = true;
            return true;
        }
        return false;
    }
    public void ScheduleMeshGeneration()
    {
        if (GenerationStarted && DataGenerated && !MeshScheduled && voxels.IsCreated && heightMaps.IsCreated)
        {
            //meshData = new MeshDataStruct();
            meshData.Initialize();

            meshJob = new ChunkMeshJob()
            {
                chunkWidth = WorldSettings.ChunkWidth,
                chunkHeight = WorldSettings.ChunkHeight,
                voxels = this.voxels,
                heightMaps = this.heightMaps,
                meshData = this.meshData
            };
            MeshScheduled = true;
            meshHandle = meshJob.Schedule();
        }
    }
    public bool CompleteMeshGeneration()
    {
        if (GenerationStarted && DataGenerated && MeshScheduled && !MeshGenerated && meshHandle.IsCompleted)
        {
            meshHandle.Complete();
            MeshScheduled = false;
            MeshGenerated = true;
            return true;
        }
        return false;
    }
    public Mesh GetMesh()
    {
        if (MeshGenerated && meshData.Initialized)
        {
            Mesh mesh = new Mesh() { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            meshData.UploadToMesh(ref mesh);
            return mesh;
        }
        return null;
    }
    public void Dispose()
    {
        dataHandle.Complete();
        meshHandle.Complete();
        meshData.Dispose();
        if(noiseParameters.IsCreated) noiseParameters.Dispose();
        if(octaveOffsets.IsCreated) octaveOffsets.Dispose();
        if(voxels.IsCreated) voxels.Dispose();
        if(heightMaps.IsCreated) heightMaps.Dispose();
    }
}


[BurstCompile]
public struct ChunkDataJob : IJob
{
    [ReadOnly]
    public float globalScale;
    [ReadOnly]
    public int chunkWidth;
    [ReadOnly]
    public int chunkHeight;
    [ReadOnly]
    public Vector3 chunkPos;
    [ReadOnly]
    public bool stress;
    [ReadOnly]
    public NativeArray<NoiseParameters> noiseParameters;
    [ReadOnly]
    public NativeArray<Vector2Int> octaveOffsets;

    public NativeArray<Voxel> voxels;
    public NativeArray<HeightMap> heightMaps;

    public void Execute()
    {
        Voxel solid = new Voxel() { ID = 1 };
        Voxel emptyVoxel = new Voxel() { ID = 0 };
        for(int x = 0; x < chunkWidth + 2; x++)
        {
            for (int z = 0; z < chunkWidth + 2; z++)
            {
                int height = 0;
                HeightMap heightMap = new HeightMap() { data = 0 };
                if (stress)
                {
                    height = GetHeigthStress((int)chunkPos.x + x, (int)chunkPos.z + z, x, z);
                }
                else
                {
                    float fHeight = 0;
                    for (int i = 0; i < noiseParameters.Length; i++)
                    {
                        fHeight += GetHeight((int)chunkPos.x * chunkWidth + x - 1, (int)chunkPos.z * chunkWidth + z - 1, noiseParameters[i]);
                    }
                    height = Mathf.FloorToInt(fHeight / noiseParameters.Length * (chunkHeight - 1));

                    if(height <= 0)
                        height = 1;

                    if(height > chunkHeight)
                    {
                        height = chunkHeight;
                    }
                }

                ReadWriteStructs.SetSolid(ref heightMap, (uint)height);
                heightMaps[getMapIndex(x, z)] = heightMap;
                for (int y = 0; y < chunkHeight; y++)
                {
                    int voxelIndex = getVoxelIndex(x, y, z);
                    voxels[voxelIndex] = emptyVoxel;
                    if (y < height)
                    {
                        voxels[voxelIndex] = solid;
                    }
                }
            }
        }
    }

    int GetHeigthStress(int x, int z, int xLocal, int zLocal)
    {
        int heigth = (xLocal + zLocal) % 2 == 0 ? chunkHeight : 1;
        return heigth;
    }

    float GetHeight(int x, int y, NoiseParameters param)
    {
        float height = 0;
        float amplitude = 1;
        float frequency = 1;

        float max = 0;
        for (int i = 0; i < param.octaves; i++)
        {
            float sampleX = (x + octaveOffsets[i].x) / param.noiseScale / globalScale * frequency;
            float sampleZ = (y + octaveOffsets[i].y) / param.noiseScale / globalScale * frequency;

            float value = Mathf.PerlinNoise(sampleX, sampleZ);
            height += value * amplitude / param.damping;
            max += amplitude;
            amplitude *= -param.persistence;
            frequency *= param.lacunarity;
        }
        height = (Mathf.Pow(Mathf.Abs(height), param.ePow) / Mathf.Pow(Mathf.Abs(max), param.ePow));
        if (height > param.maxHeight)
            height = param.maxHeight;
        return height * param.blending;
    }
    int getVoxelIndex(int x, int y, int z)
    {
        return z + (y * (chunkWidth + 2)) + (x * (chunkWidth + 2) * chunkHeight);
    }

    int getMapIndex(int x, int z)
    {
        return z + x * (chunkWidth + 2);
    }
}

[Serializable]
public struct MeshDataStruct
{
    public NativeArray<Vector3> vertices;
    public NativeArray<int> indices;
    public NativeArray<Vector3> normals;
    public NativeArray<Vector2> uvs;
    public NativeArray<Color> colors32;

    public NativeArray<int> count;

    public bool Initialized;
    public void Initialize()
    {
        if(Initialized)
            Dispose();
        Initialized = true;
        count = new NativeArray<int>(2, Allocator.Persistent);
        vertices = new NativeArray<Vector3>(WorldSettings.RenderedVoxelsPerChunk * 6 * 4, Allocator.Persistent);
        indices = new NativeArray<int>(WorldSettings.RenderedVoxelsPerChunk * 6 * 6, Allocator.Persistent);
        normals = new NativeArray<Vector3>(WorldSettings.RenderedVoxelsPerChunk * 6 * 4, Allocator.Persistent);
        uvs = new NativeArray<Vector2>(WorldSettings.RenderedVoxelsPerChunk * 6 * 4, Allocator.Persistent);
        colors32 = new NativeArray<Color>(WorldSettings.RenderedVoxelsPerChunk * 6 * 4, Allocator.Persistent);
    }
    public void Dispose()
    {
        Initialized = false;
        if(vertices.IsCreated) vertices.Dispose();
        if(indices.IsCreated) indices.Dispose();
        if(normals.IsCreated) normals.Dispose();
        if(uvs.IsCreated) uvs.Dispose();
        if(colors32.IsCreated) colors32.Dispose();
        if(count.IsCreated) count.Dispose();
    }

    public void UploadToMesh(ref Mesh mesh)
    {
        if (Initialized && mesh != null)
        {
            mesh.SetVertices(vertices, 0, count[0]);
            mesh.SetIndices(indices, 0, count[1], MeshTopology.Triangles, 0);
            mesh.SetNormals(normals, 0, count[0]);
            mesh.SetUVs(0, uvs, 0, count[0]);
            mesh.SetColors(colors32, 0, count[0]);

            mesh.RecalculateTangents();
            mesh.Optimize();
        }
    }
}

[BurstCompile]
public struct ChunkMeshJob : IJob
{
    #region Input

    [ReadOnly]
    public int chunkWidth;
    [ReadOnly]
    public int chunkHeight;
    [ReadOnly]
    public NativeArray<Voxel> voxels;
    [ReadOnly]
    public NativeArray<HeightMap> heightMaps;
    #endregion

    #region Output

    public MeshDataStruct meshData;
    #endregion

    #region Methods

    int getVoxelIndex(int x, int y, int z)
    {
        return z + (y * (chunkWidth + 2)) + (x * (chunkWidth + 2) * chunkHeight);
    }

    int getMapIndex(int x, int z)
    {
        return z + (x * (chunkWidth + 2));
    }
    #endregion

    #region Execution

    public void Execute()
    {
        #region Allocations
        NativeArray<Vector3> Vertices = new NativeArray<Vector3>(8, Allocator.Temp)
        {
            [0] = new Vector3(0, 1, 0), //0
            [1] = new Vector3(1, 1, 0), //1
            [2] = new Vector3(1, 1, 1), //2
            [3] = new Vector3(0, 1, 1), //3

            [4] = new Vector3(0, 0, 0), //4
            [5] = new Vector3(1, 0, 0), //5
            [6] = new Vector3(1, 0, 1), //6
            [7] = new Vector3(0, 0, 1) //7
        };
        NativeArray<Vector3> FaceCheck = new NativeArray<Vector3>(6, Allocator.Temp)
        {
            [0] = new Vector3(0, 0, -1), //back 0
            [1] = new Vector3(1, 0, 0), //right 1
            [2] = new Vector3(0, 0, 1), //front 2
            [3] = new Vector3(-1, 0, 0), //left 3
            [4] = new Vector3(0, 1, 0), //top 4
            [5] = new Vector3(0, -1, 0) //bottom 5
        };

        NativeArray<int> FaceVerticeIndex = new NativeArray<int>(24, Allocator.Temp)
        {
            [0] = 4,
            [1] = 5,
            [2] = 1,
            [3] = 0,
            [4] = 5,
            [5] = 6,
            [6] = 2,
            [7] = 1,
            [8] = 6,
            [9] = 7,
            [10] = 3,
            [11] = 2,
            [12] = 7,
            [13] = 4,
            [14] = 0,
            [15] = 3,
            [16] = 0,
            [17] = 1,
            [18] = 2,
            [19] = 3,
            [20] = 7,
            [21] = 6,
            [22] = 5,
            [23] = 4,
        };

        NativeArray<Vector2> VerticeUVs = new NativeArray<Vector2>(5, Allocator.Temp)
        {
            [0] = new Vector2(0, 0),
            [1] = new Vector2(1, 0),
            [2] = new Vector2(1, 1),
            [3] = new Vector2(0, 1)
        };

        NativeArray<int> FaceIndices = new NativeArray<int>(6, Allocator.Temp)
        {
            [0] = 0,
            [1] = 3,
            [2] = 2,
            [3] = 0,
            [4] = 2,
            [5] = 1
        };
        #endregion

        #region Execution

        for (int x = 1; x <= chunkWidth; x++)
        {
            for (int z = 1; z <= chunkWidth; z++)
            {
                int maxHeight = (int)(ReadWriteStructs.GetSolid(heightMaps[getMapIndex(x, z)])) - 1;
                for (int y = maxHeight; y >= 0; y--)
                {
                    Voxel voxel = voxels[getVoxelIndex(x, y, z)];
                    if (ReadWriteStructs.GetVoxelType(voxel) == 0)
                        continue;

                    Vector3 voxelPos = new Vector3(x - 1, y, z - 1);

                    bool surrounded = true;

                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 face = FaceCheck[i];

                        if (!(y == chunkHeight - 1 && i == 4)) // highest and top face
                        {
                            if (y == 0 && i == 5) // lowest and bottom face
                                continue;
                            int faceCheckIndex = getVoxelIndex(x + (int)face.x, y + (int)face.y, z + (int)face.z);
                            if (ReadWriteStructs.GetVoxelType(voxels[faceCheckIndex]) != 0)
                                continue;
                        }
                        surrounded = false;
                        for (int j = 0; j < 4; j++)
                        {
                            meshData.vertices[meshData.count[0] + j] = Vertices[FaceVerticeIndex[i * 4 + j]] + voxelPos;
                            meshData.uvs[meshData.count[0] + j] = VerticeUVs[j];
                            meshData.normals[meshData.count[0] + j] = FaceCheck[i];
                            //meshData.colors32[meshData.count[0]] = new Color32((byte)x, (byte)y, (byte)z, 255);
                            //meshData.colors32[meshData.count[0]] = new Color(((float)x) / chunkWidth, ((float)y) / chunkHeight, ((float)z) / chunkWidth, 255f);
                            float color = ((float)(y) / chunkHeight);
                            meshData.colors32[meshData.count[0] + j] = new Color(color, 0, 0, 0);
                        }
                        for(int k = 0; k < 6; k++)
                        {
                            meshData.indices[meshData.count[1] + k] = meshData.count[0] + FaceIndices[k];
                        }
                        meshData.count[0] += 4;
                        meshData.count[1] += 6;
                    }
                    if (surrounded)
                        y = -1;
                }
            }
        }
        #endregion

        #region Deallocations

        if (Vertices.IsCreated) Vertices.Dispose();
        if (FaceCheck.IsCreated) FaceCheck.Dispose();
        if (FaceVerticeIndex.IsCreated) FaceVerticeIndex.Dispose();
        if (VerticeUVs.IsCreated) VerticeUVs.Dispose();
        if (FaceIndices.IsCreated) FaceIndices.Dispose();
        #endregion
    }
    #endregion
}
