using UnityEngine;


[CreateAssetMenu(fileName = "World Setting", menuName = "ScriptableObjects/WorldSetting")]
public class WorldSetting : ScriptableObject
{
    public int ChunkWidth;
    public int ChunkHeight;
    public int HeightStep;

    public int LoadDistance;
    public int SimulateDistance;
    public int RenderDistance;

    public int ChunksProcessedPerFrame;
    public int ChunksToLoadPerFrame;
    public bool StressTest;
    public bool Disposing;

    public GenerationMode Mode;
}