using UnityEngine;

public enum GenerationMode
{
    CPU = 0,
    GPU = 1
};
public static class WorldSettings
{
    public static GenerationMode Mode = GenerationMode.CPU;
    public static int Seed {  get; private set; }
    public static int ChunkWidth { get; private set; }
    public static int ChunkHeight {  get; private set; }
    public static float HeightStep { get; private set; }
    public static int LoadDistance {  get; private set; }
    public static int SimulateDistance { get; private set; }
    public static int RenderDistance {  get; private set; }
    public static int ChunksProcessedPerFrame { get; private set; }
    public static int ChunksToLoadPerFrame { get; private set; }
    public static bool StressTest { get; private set; }
    public static bool Disposing { get; private set; }
    public static int VoxelsPerChunk { get; private set; }
    public static int RenderedVoxelsPerChunk { get; private set; }


    public static bool Initialized = false;
    public static void Initialize(WorldSetting setting, int seed)
    {
        if (setting != null)
        {
            Seed = seed;
            ChunkWidth = setting.ChunkWidth <= 0 ? 32 : setting.ChunkWidth;
            ChunkHeight = setting.ChunkHeight <= 0 ? 64 : setting.ChunkHeight;
            HeightStep = setting.HeightStep <= 0 ? 1 : setting.HeightStep;
            LoadDistance = setting.LoadDistance <= 0 ? 6 : setting.LoadDistance;
            SimulateDistance = setting.SimulateDistance <= 0 || setting.SimulateDistance > LoadDistance ? LoadDistance : setting.SimulateDistance;
            RenderDistance = setting.RenderDistance <= 0 || setting.RenderDistance > LoadDistance ? LoadDistance : setting.RenderDistance;
            ChunksProcessedPerFrame = setting.ChunksProcessedPerFrame <= 0 || setting.ChunksProcessedPerFrame > RenderDistance * RenderDistance ? 5 : setting.ChunksProcessedPerFrame;
            ChunksToLoadPerFrame = setting.ChunksToLoadPerFrame <= 0 || setting.ChunksToLoadPerFrame > RenderDistance * RenderDistance ? 5 : setting.ChunksToLoadPerFrame;
            VoxelsPerChunk = (ChunkWidth + 2) * ChunkHeight * (ChunkWidth + 2);
            RenderedVoxelsPerChunk = ChunkWidth * ChunkHeight * ChunkWidth;
            StressTest = setting.StressTest;
            Disposing = setting.Disposing;
            Mode = setting.Mode; 
            Initialized = true;
        }
    }

    public static bool ChunksInRange(Vector3 center, Vector3 position, int range)
    {
        return position.x <= center.x + range &&
            position.x >= center.x - range &&
            position.z <= center.z + range &&
            position.z >= center.z - range;
    }

    public static Vector3 ChunkPositionFromPosition(Vector3 pos)
    {
        pos /= ChunkWidth;
        pos.x = Mathf.FloorToInt(pos.x);
        pos.y = 0;
        pos.z = Mathf.FloorToInt(pos.z);
        return pos;
    }
}
