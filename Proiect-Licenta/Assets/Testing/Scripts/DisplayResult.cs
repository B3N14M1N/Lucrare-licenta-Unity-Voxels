using Unity.Mathematics;
using UnityEngine;

public class DisplayResult : MonoBehaviour
{
    private Vector2Int offset = new Vector2Int(0, 0);
    private Vector2Int prevOffset = new Vector2Int(0, 0);
    private ComputeBuffer noiseBuffer;
    private ComputeBuffer octavesBuffer;
    private RenderTexture renderTexture;
    private Material material;
    private float zoom = 1;
    [Header("Fields")]
    public ComputeShader computeShader;
    [Range(8,32)]
    public int ChunkWidth = 8;
    private int prvWidth;
    public int ChunkHeight = 64;
    [Range(1,100)]
    public int ChunksRender = 12;
    private int prvRender;

    public float ZoomSensitivity = 1f;
    public float MoveSensitivity = 1f;

    [Header("Noise")]
    public uint seed;
    public NoiseParametersScriptableObject noiseParameters;
    private Vector2Int[] octaveOffsets; 

    [Header("Result")]
    public GameObject displayObject;


    public void Start()
    {
        material = displayObject.GetComponent<MeshRenderer>().sharedMaterial;
        Application.targetFrameRate = 30;

        prvWidth = ChunkWidth;
        prvRender = ChunksRender;
        CreateRendererTexture();
        material.SetTexture("_Texture2D", renderTexture);
        zoom = 1;
        offset = Vector2Int.zero;
        prevOffset = offset;

    }
    public void InitSeed()
    {
        uint octavesMax = 0;
        for (int i = 0; i < noiseParameters.noise.Count; i++)
        {
            if (noiseParameters.noise[i].octaves > octavesMax)
                octavesMax = noiseParameters.noise[i].octaves;
        }
        octaveOffsets = new Vector2Int[octavesMax];
        System.Random rnd = new System.Random((int)seed);
        for (int i = 0; i < octavesMax; i++)
        {
            octaveOffsets[i] = new Vector2Int(rnd.Next(-10000,10000),rnd.Next(-10000,10000));
        }
        octavesBuffer?.Dispose();
        octavesBuffer = new ComputeBuffer((int)octavesMax,8);
        octavesBuffer.SetData(octaveOffsets);
    }

    public void RenderTexture()
    {
        InitSeed();
        noiseBuffer?.Dispose();
        noiseBuffer = new ComputeBuffer(noiseParameters.noise.Count, 8 * 4);
        noiseBuffer.SetData(noiseParameters.noise);

        computeShader.SetInt("noiseParametersCount", noiseParameters.noise.Count);
        computeShader.SetInt("chunkHeight", ChunkHeight);

        computeShader.SetFloat("globalScale", noiseParameters.globalScale);

        computeShader.SetVector("offset", (Vector2)offset);

        computeShader.SetBuffer(0, "octaveOffsets", octavesBuffer);
        computeShader.SetBuffer(0, "noiseParameters", noiseBuffer);
        computeShader.SetTexture(0, "Result", renderTexture);

        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
        noiseBuffer.Dispose();
    }

    public void Update()
    {
        if (prvWidth != ChunkWidth || prvRender != ChunksRender)
        {
            CreateRendererTexture();
            material.SetTexture("_Texture2D", renderTexture);
            prvWidth = ChunkWidth;
            prvRender = ChunksRender;
        }
        if (Input.GetMouseButton(1))
        {
            offset += new Vector2Int((int)(Input.GetAxisRaw("Mouse X") * MoveSensitivity), (int)(Input.GetAxisRaw("Mouse Y") * MoveSensitivity));
            RenderTexture();
        }

        zoom += Input.mouseScrollDelta.y * ZoomSensitivity;
        zoom = Mathf.Clamp(zoom, 1f, 10f);
        material.SetFloat("_Zoom", zoom);
    }

    public void CreateRendererTexture()
    {
        if (renderTexture != null)
        {
            Destroy(renderTexture);
        }
        renderTexture = new RenderTexture((ChunksRender * 2 + 1) * ChunkWidth, (ChunksRender * 2 + 1) * ChunkWidth, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();
    }



    public void OnApplicationQuit()
    {
        noiseBuffer?.Dispose();
        octavesBuffer?.Dispose();
        if (renderTexture != null)
        {
            Destroy(renderTexture);
        }
    }
}
