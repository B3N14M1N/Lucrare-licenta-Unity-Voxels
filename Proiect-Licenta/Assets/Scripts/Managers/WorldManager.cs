using System;
using UnityEditor;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    [Header("World Settings")]
    public int seed;
    public WorldSetting worldSetting;

    [Header("Player")]
    public Transform player;

    void Start()
    {
        WorldSettings.Initialize(worldSetting, seed);
        ChunksManager.Instance.Initialize(WorldSettings.ChunkPositionFromPosition(player.transform.position) + Vector3.forward);
        ChunkFactory.Instance.Initialize(WorldSettings.ChunksProcessedPerFrame);
    }

    // Update is called once per frame
    void Update()
    {
        var currentPosition = WorldSettings.ChunkPositionFromPosition(player.transform.position);
        if (ChunksManager.Instance.Center != currentPosition)
        {
            ChunksManager.Instance.UpdateChunks(currentPosition);
        }
    }

    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect();
#endif
    }
}
