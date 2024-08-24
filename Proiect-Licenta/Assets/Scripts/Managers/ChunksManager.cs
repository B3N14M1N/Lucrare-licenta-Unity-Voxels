using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ChunksManager : MonoBehaviour
{
    #region Fields

    [Header("Buffers")]
    private Queue<Chunk> poolChunks;
    private Dictionary<Vector3, Chunk> chunkCache;
    private Dictionary<Vector3, Chunk> activeChunks;

    public int poolSize;
    public int cacheSize;
    public int activeSize;
    public int totalChunks;

    public Vector3 Center {  get; private set; }

    #endregion

    #region Initialization
    public void Initialize(Vector3 center)
    {
        Center = center;
        Initialize();
    }

    public void Initialize()
    {
        chunkCache = new Dictionary<Vector3, Chunk>();
        activeChunks = new Dictionary<Vector3, Chunk>();
        poolChunks = new Queue<Chunk>();

        for (int i = 0; i < WorldSettings.LoadDistance * WorldSettings.LoadDistance; i++)
        {
            poolChunks.Enqueue(ChunkFactory.Instance.CreateChunkObject());
        }
    }

    private static ChunksManager instance;
    public static ChunksManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<ChunksManager>();
            if (instance == null)
                instance = new ChunksManager();
            return instance;
        }
        private set
        {
            instance = value;
        }
    }
    #endregion

    #region Mono

    public void Update()
    {
        cacheSize = chunkCache.Count;
        activeSize = activeChunks.Count;
        poolSize = poolChunks.Count;
        totalChunks = cacheSize + activeSize + poolSize;
    }
    #endregion

    #region Methods
    public void UpdateChunks(Vector3 center)
    {
        chunkCache.AddRange(activeChunks);
        activeChunks.Clear();

        for (int x = (int)center.x - WorldSettings.RenderDistance; x <= (int)center.x + WorldSettings.RenderDistance; x++)
        {
            for (int z = (int)center.z - WorldSettings.RenderDistance; z <= (int)center.z + WorldSettings.RenderDistance; z++)
            {
                var pos = new Vector3(x, 0, z);
                Chunk chunk = GetChunkFromSourceAndRemove(pos, ref chunkCache);

                if (chunk == null) 
                {
                    chunk = CreateChunk(pos);
                }

                //if (!chunk.Initialized)
                //    chunk.InitializeChunk();

                activeChunks.Add(pos, chunk);
                chunk.gameObject.SetActive(true);
            }
        }

        List<Vector3> removals = (from key in chunkCache.Keys
                                  where WorldSettings.ChunksInRange(center, key, WorldSettings.LoadDistance) == false
                                  select key).ToList();
        foreach (var key in removals)
        {
            ClearChunkAndEnqueue(key, ref chunkCache);
        }

        foreach (var key in chunkCache.Keys)
        {
            chunkCache[key].gameObject.SetActive(false);
        }
        this.Center = center;
    }

    public void UpdateChunks()
    {
        UpdateChunks(Center);
    }

    public Chunk CreateChunk(Vector3 pos)
    {
        Chunk chunk = (poolChunks.Count > 0) ? poolChunks.Dequeue() : ChunkFactory.Instance.CreateChunkObject();
        ChunkFactory.Instance.UpdateChunk(pos, chunk);
        ChunkFactory.Instance.GenerateChunkData(chunk);
        return chunk;
    }

    public Chunk GetChunkFromSource(Vector3 pos, ref Dictionary<Vector3, Chunk> source)
    {
        if (source == null)
            return null;
        source.TryGetValue(pos, out Chunk chunk);
        return chunk;
    }

    public Chunk GetChunk(Vector3 pos)
    {
        Chunk chunk = GetChunkFromSource(pos, ref chunkCache);
        if (chunk == null)
            chunk = GetChunkFromSource(pos, ref activeChunks);
        return chunk;
    }

    public Chunk GetChunkFromSourceAndRemove(Vector3 pos, ref Dictionary<Vector3, Chunk> source)
    {
        if (source.TryGetValue(pos, out Chunk chunk))
        {
            source.Remove(pos);
        }
        return chunk;
    }

    public void ClearChunkAndEnqueue(Vector3 pos, ref Dictionary<Vector3, Chunk> source)
    {
        if (source.TryGetValue(pos, out Chunk chunk))
        {
            chunk.gameObject.SetActive(false);
            chunk.ClearChunk();
            chunk.gameObject.name = "Chunk [pool]";
            poolChunks.Enqueue(chunk);
            source.Remove(pos);
        }
    }

    public (int, int) GetAllMeshesVerticesAndTriangles()
    {
        int vertices = 0;
        int triangles = 0;

        foreach (Chunk chunk in activeChunks.Values)
        {
            (var vertices1, var triangles1) = chunk.GetMeshVerticesAndTrianglesCount();
            vertices += vertices1;
            triangles += triangles1;
        }

        return (vertices, triangles);
    }
    #endregion

    #region Dispose

    public void ClearAllChunksData()
    {
        List<Vector3> removals = (from key in chunkCache.Keys
                                  select key).ToList();
        foreach (var key in removals)
        {
            ClearChunkAndEnqueue(key, ref chunkCache);
        }
        removals = (from key in activeChunks.Keys
                    select key).ToList();
        foreach (var key in removals)
        {
            ClearChunkAndEnqueue(key, ref activeChunks);
        }
    }

    public void Dispose()
    {
        while (poolChunks.Count > 0)
        {
            GameObject chunkObject = poolChunks.Peek().gameObject;
            Chunk chunk = poolChunks.Dequeue();
            chunk.Dispose();
            GameObject.DestroyImmediate(chunk);
            GameObject.DestroyImmediate(chunkObject);
        }
        poolChunks.Clear();

        List<Vector3> removals = activeChunks.Keys.ToList();
        foreach (var key in removals)
        {
            GameObject chunk = activeChunks[key].gameObject;
            activeChunks[key].Dispose();
            GameObject.DestroyImmediate(activeChunks[key]);
            GameObject.DestroyImmediate(chunk);
            activeChunks.Remove(key);
        }
        activeChunks.Clear();

        removals = chunkCache.Keys.ToList();
        foreach (var key in removals)
        {
            GameObject chunk = chunkCache[key].gameObject;
            chunkCache[key].Dispose();
            GameObject.DestroyImmediate(chunkCache[key]);
            GameObject.DestroyImmediate(chunk);
            chunkCache.Remove(key);
        }
        chunkCache.Clear();

#if UNITY_EDITOR
        EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect();
#endif

    }

    private void OnApplicationQuit()
    {
        Dispose();
    }
    #endregion
}
