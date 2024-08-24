using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunkFactory : MonoBehaviour
{
    #region Fields

    [Header("Compute Shaders")]
    private int xThreads;
    private int yThreads;

    public int numberMeshBuffers = 0;
    public ComputeShader voxelHeightGenerator;
    public ComputeShader voxelDataGenerator;
    public ComputeShader voxelMeshGenerator;

    [Header("Buffers")]
    private Queue<ChunkMeshBuffer> availableMeshComputeBuffers = new Queue<ChunkMeshBuffer>();
    private Queue<ChunkDataBuffer> availableNoiseComputeBuffers = new Queue<ChunkDataBuffer>();

    public Queue<Chunk> chunksToProccess = new Queue<Chunk>();

    public Queue<JobChunkGenerator> jobsFinished = new Queue<JobChunkGenerator>();
    public List<JobChunkGenerator> jobsGenerating = new List<JobChunkGenerator>();

    public int ChunksToProcessCount;
    [Header("Materials")]
    public List<Material> materials = new List<Material>();
    public int materialIndex = 0;
    public Material Material {  get { return materials[materialIndex]; } }
    public bool canChangeMaterial = true;

    #endregion

    #region Initioalization

    private static ChunkFactory instance;
    public static ChunkFactory Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<ChunkFactory>();
            if (instance == null)
                instance = new ChunkFactory();
            return instance;
        }
        private set
        {
            instance = value;
        }
    }

    public void Initialize(int count = 18)
    {
        xThreads = (WorldSettings.ChunkWidth + 2) / 8 + 1;
        yThreads = WorldSettings.ChunkHeight / 8;
        //yThreads = 1;
        voxelHeightGenerator.SetInt("seed", WorldSettings.Seed);
        voxelHeightGenerator.SetInt("chunkWidth", WorldSettings.ChunkWidth);
        voxelHeightGenerator.SetInt("chunkHeight", WorldSettings.ChunkHeight);

        voxelDataGenerator.SetInt("chunkWidth", WorldSettings.ChunkWidth);
        voxelDataGenerator.SetInt("chunkHeight", WorldSettings.ChunkHeight);


        voxelMeshGenerator.SetInt("chunkWidth", WorldSettings.ChunkWidth);
        voxelMeshGenerator.SetInt("chunkHeight", WorldSettings.ChunkHeight);

        for (int i = 0; i < count; i++)
        {
            CreateNewNoiseBuffer();
            CreateNewMeshBuffer();
        }
    }

    #endregion

    #region Mono

    public void Update()
    {
        ChunksToProcessCount = chunksToProccess.Count;
        numberMeshBuffers = availableMeshComputeBuffers.Count;

        if (Input.GetKeyUp(KeyCode.Alpha1) && canChangeMaterial)
        {
            materialIndex = materialIndex == materials.Count - 1 ? 0 : ++materialIndex;
        }

        for (int i = 0; i <= WorldSettings.ChunksToLoadPerFrame && jobsFinished.Count > 0; i++)
        {
            var job = jobsFinished.Dequeue();
            Chunk chunk = ChunksManager.Instance.GetChunk(job.chunkPos);
            if (chunk != null)
            {
                Mesh mesh = job.GetMesh();
                if (mesh != null)
                    chunk.UploadMesh(ref mesh);
            }
            job.Dispose();
        }

        for (int i = 0; i < WorldSettings.ChunksProcessedPerFrame && chunksToProccess.Count > 0; i++)
        {
            Chunk chunk = chunksToProccess.Dequeue();
            if (chunk != null && WorldSettings.ChunksInRange(ChunksManager.Instance.Center, chunk.chunkPosition, WorldSettings.LoadDistance))
            {
                if (WorldSettings.Mode == GenerationMode.GPU)
                {
                    GenerateVoxelData(chunk.chunkPosition);
                }
                else
                {
                    JobChunkGenerator job = new JobChunkGenerator(chunk.chunkPosition);
                    jobsGenerating.Add(job);
                }
            }
        }

        for(int i = 0; i < jobsGenerating.Count; i++)
        {
            var job = jobsGenerating[i];
            if (job != null)
            {
                if (job.CompleteDataGeneration())
                {
                    // get voxels and map
                    job.ScheduleMeshGeneration();
                }
                if (job.CompleteMeshGeneration())
                {
                    jobsFinished.Enqueue(job);
                    jobsGenerating.RemoveAt(i);
                    i--;
                }

            }
        }
    }

    #endregion

    #region Chunk GameObject Creation
    public Chunk CreateChunkObject()
    {
        GameObject chunkObject = new GameObject();
        chunkObject.transform.SetParent(transform);
        return chunkObject.AddComponent<Chunk>();
    }

    public void GenerateChunkData(Chunk chunk)
    {
        chunksToProccess.Enqueue(chunk);
    }

    public void UpdateChunk(Vector3 pos, Chunk chunk)
    {
        chunk.chunkPosition = pos;
        chunk.transform.name = "Chunk [" + (int)pos.x + "]:[" + (int)pos.z + "]";
        chunk.transform.localPosition = pos * WorldSettings.ChunkWidth;
    }
    #endregion

    #region Chunk Creation
    private void ClearVoxelData1(ChunkDataBuffer buffer)
    {
        voxelDataGenerator.SetBuffer(1, "voxels", buffer.voxelsBuffer);
        voxelDataGenerator.SetBuffer(1, "map", buffer.mapBuffer);
        voxelDataGenerator.Dispatch(1, xThreads, yThreads, xThreads);
    }
    private void ClearVoxelData(ChunkDataBuffer buffer)
    {
        voxelDataGenerator.SetBuffer(1, "voxels", buffer.voxelsBuffer);
        voxelDataGenerator.SetBuffer(1, "map", buffer.mapBuffer);
        voxelDataGenerator.Dispatch(1, xThreads, yThreads, xThreads);
        AsyncGPUReadback.Request(buffer.mapBuffer, (callback) => { });
    }

    public void GenerateVoxelDataAsyncNotSupported(Vector3 pos)
    {
        ChunkDataBuffer noiseBuffer = GetNoiseBuffer();
        noiseBuffer.InitializeBuffer();

        voxelDataGenerator.SetVector("chunkPosition", pos);
        voxelDataGenerator.SetBuffer(1, "map", noiseBuffer.mapBuffer);
        voxelDataGenerator.Dispatch(1, xThreads, 1, xThreads);

        voxelDataGenerator.SetBuffer(0, "voxels", noiseBuffer.voxelsBuffer);
        voxelDataGenerator.SetBuffer(0, "map", noiseBuffer.mapBuffer);
        voxelDataGenerator.Dispatch(0, xThreads, yThreads, xThreads);

        ChunkMeshBuffer meshBuffer = GetMeshBuffer();
        meshBuffer.InitializeBuffer();

        meshBuffer.countBuffer.SetCounterValue(0);
        meshBuffer.countBuffer.SetData(new uint[] { 0, 0 });
        voxelMeshGenerator.SetBuffer(0, "counter", meshBuffer.countBuffer);

        voxelMeshGenerator.SetBuffer(0, "voxelArray", noiseBuffer.voxelsBuffer);
        voxelMeshGenerator.SetBuffer(0, "map", noiseBuffer.mapBuffer);
        voxelMeshGenerator.SetBuffer(0, "vertexBuffer", meshBuffer.vertexBuffer);
        voxelMeshGenerator.SetBuffer(0, "normalBuffer", meshBuffer.normalBuffer);
        voxelMeshGenerator.SetBuffer(0, "indexBuffer", meshBuffer.indexBuffer);
        voxelMeshGenerator.SetBuffer(0, "uvBuffer", meshBuffer.uvBuffer);
        voxelMeshGenerator.SetBuffer(0, "colorBuffer", meshBuffer.colorBuffer);

        voxelMeshGenerator.Dispatch(0, xThreads, yThreads, xThreads);

        var thisChunk = ChunksManager.Instance.GetChunk(pos);
        if (thisChunk != null)
        {
            thisChunk.UploadMeshBuffer(ref meshBuffer);
        }
        ClearAndRequeueBuffer(noiseBuffer);
        ClearAndRequeueBuffer(meshBuffer);
    }
    public void GenerateVoxelData(Vector3 pos)
    {
        ChunkDataBuffer noiseBuffer = GetNoiseBuffer();
        noiseBuffer.InitializeBuffer();

        voxelHeightGenerator.SetVector("chunkPosition", pos);
        voxelHeightGenerator.SetBuffer(0, "map", noiseBuffer.mapBuffer);
        voxelHeightGenerator.Dispatch(0, xThreads, 1, xThreads);

        AsyncGPUReadback.Request(noiseBuffer.mapBuffer, (callback) =>
        {
            voxelDataGenerator.SetBuffer(0, "voxels", noiseBuffer.voxelsBuffer);
            voxelDataGenerator.SetBuffer(0, "map", noiseBuffer.mapBuffer);
            voxelDataGenerator.Dispatch(0, xThreads, yThreads, xThreads);

            AsyncGPUReadback.Request(noiseBuffer.mapBuffer, (callback) =>
            {
                ChunkMeshBuffer meshBuffer = GetMeshBuffer();
                meshBuffer.InitializeBuffer();

                meshBuffer.countBuffer.SetCounterValue(0);
                meshBuffer.countBuffer.SetData(new uint[] { 0, 0 });
                voxelMeshGenerator.SetBuffer(0, "counter", meshBuffer.countBuffer);

                voxelMeshGenerator.SetBool("stressTest", WorldSettings.StressTest);
                voxelMeshGenerator.SetBuffer(0, "voxelArray", noiseBuffer.voxelsBuffer);
                voxelMeshGenerator.SetBuffer(0, "map", noiseBuffer.mapBuffer);
                voxelMeshGenerator.SetBuffer(0, "vertexBuffer", meshBuffer.vertexBuffer);
                voxelMeshGenerator.SetBuffer(0, "normalBuffer", meshBuffer.normalBuffer);
                voxelMeshGenerator.SetBuffer(0, "indexBuffer", meshBuffer.indexBuffer);
                voxelMeshGenerator.SetBuffer(0, "uvBuffer", meshBuffer.uvBuffer);
                voxelMeshGenerator.SetBuffer(0, "colorBuffer", meshBuffer.colorBuffer);


                voxelMeshGenerator.Dispatch(0, xThreads, yThreads, xThreads);

                AsyncGPUReadback.Request(meshBuffer.countBuffer, (callback) =>
                {
                    var thisChunk = ChunksManager.Instance.GetChunk(pos);
                    if (thisChunk != null)
                    {
                        thisChunk.UploadMeshBuffer(ref meshBuffer);
                    }
                    ClearAndRequeueBuffer(noiseBuffer);
                    ClearAndRequeueBuffer(meshBuffer);
                });
            });
        });
    }
    #endregion

    #region ChunkDataBuffer Pooling
    public ChunkDataBuffer GetNoiseBuffer()
    {
        if (availableNoiseComputeBuffers.Count > 0)
        {
            return availableNoiseComputeBuffers.Dequeue();
        }
        else
        {
            return CreateNewNoiseBuffer(false);
        }
    }

    public ChunkDataBuffer CreateNewNoiseBuffer(bool enqueue = true)
    {
        ChunkDataBuffer buffer = new ChunkDataBuffer();

        if (enqueue)
            availableNoiseComputeBuffers.Enqueue(buffer);

        return buffer;
    }

    public void ClearAndRequeueBuffer(ChunkDataBuffer buffer)
    {
        //buffer.Dispose();
        ClearVoxelData(buffer);
        availableNoiseComputeBuffers.Enqueue(buffer);
    }
    #endregion

    #region MeshBuffer Pooling

    public ChunkMeshBuffer GetMeshBuffer()
    {
        if (availableMeshComputeBuffers.Count > 0)
        {
            return availableMeshComputeBuffers.Dequeue();
        }
        else
        {
            //Debug.Log("Generate container");
            return CreateNewMeshBuffer(false);
        }
    }

    public ChunkMeshBuffer CreateNewMeshBuffer(bool enqueue = true)
    {
        ChunkMeshBuffer buffer = new ChunkMeshBuffer();

        if (enqueue)
            availableMeshComputeBuffers.Enqueue(buffer);

        return buffer;
    }

    public void ClearAndRequeueBuffer(ChunkMeshBuffer buffer)
    {
        //buffer.Dispose();
        availableMeshComputeBuffers.Enqueue(buffer);
    }
    #endregion

    #region Disposing

    public void Dispose()
    {
        while(availableNoiseComputeBuffers.Count > 0)
            availableNoiseComputeBuffers.Dequeue().Dispose();
        availableNoiseComputeBuffers.Clear();

        while (availableMeshComputeBuffers.Count > 0)
            availableMeshComputeBuffers.Dequeue().Dispose();
        availableMeshComputeBuffers.Clear();

        while (chunksToProccess.Count > 0)
        {
            Chunk chunk = chunksToProccess.Dequeue();
            if (chunk != null)
                GameObject.DestroyImmediate(chunk);
        }
        while (jobsFinished.Count > 0)
        {
            jobsFinished.Dequeue().Dispose();
        }

        for (int i = 0; i < jobsGenerating.Count; i++)
        {
            jobsGenerating[i].Dispose();
        }
    }

    private void OnApplicationQuit()
    {
        Dispose();
    }

    #endregion
}

[Serializable]
public struct ChunkDataBuffer
{
    public ComputeBuffer voxelsBuffer;
    public ComputeBuffer mapBuffer;

    public bool Initialized;

    public void InitializeBuffer()
    {
        if (!Initialized)
        {
            voxelsBuffer = new ComputeBuffer(WorldSettings.VoxelsPerChunk, 4);
            mapBuffer = new ComputeBuffer((WorldSettings.ChunkWidth + 2) * (WorldSettings.ChunkWidth + 2), 4);
        }
        Initialized = true;
    }

    public void Dispose()
    {
        voxelsBuffer?.Dispose();
        mapBuffer?.Dispose();

        Initialized = false;
    }
};

[Serializable]
public struct ChunkMeshBuffer
{
    public ComputeBuffer vertexBuffer;
    public ComputeBuffer indexBuffer;
    public ComputeBuffer normalBuffer;
    public ComputeBuffer uvBuffer;
    public ComputeBuffer colorBuffer;

    public ComputeBuffer countBuffer;

    public bool Initialized;

    public void InitializeBuffer()
    {
        if (!Initialized)
        {
            //Dispose();

            countBuffer = new ComputeBuffer(2, 4, ComputeBufferType.Counter);
            countBuffer.SetCounterValue(0);
            countBuffer.SetData(new uint[] { 0, 0 });

            int maxBlocks = WorldSettings.RenderedVoxelsPerChunk;

            vertexBuffer = new ComputeBuffer(maxBlocks * 6 * 4, 12);
            normalBuffer = new ComputeBuffer(maxBlocks * 6 * 4, 12);
            colorBuffer = new ComputeBuffer(maxBlocks * 6 * 4, 16);
            indexBuffer = new ComputeBuffer(maxBlocks * 6 * 6, 4);
            uvBuffer = new ComputeBuffer(maxBlocks * 6 * 4, 8);

        }
        Initialized = true;
    }
    public void Dispose()
    {
        vertexBuffer?.Dispose();
        normalBuffer?.Dispose();
        colorBuffer?.Dispose();
        indexBuffer?.Dispose();
        countBuffer?.Dispose();
        uvBuffer?.Dispose();

        Initialized = false;

    }
};
