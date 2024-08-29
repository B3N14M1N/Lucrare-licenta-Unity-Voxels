using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public bool Initialized { get; private set; }

    #region Fields

    [Header("Components")]
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    [Header("Chunk data")]
    public Vector3 chunkPosition;
    private MeshData meshData;
    public Mesh mesh;
    #endregion

    #region Initialization

    public void Start()
    {
        InitializeComponents();
        InitializeChunk();
    }

    public void InitializeComponents()
    {
        if (!TryGetComponent<MeshRenderer>(out meshRenderer))
        {
            meshRenderer = transform.AddComponent<MeshRenderer>();
        }
        meshRenderer.sharedMaterial = ChunkFactory.Instance.Material;

        if (!TryGetComponent<MeshFilter>(out meshFilter))
        {
            meshFilter = transform.AddComponent<MeshFilter>();
        }

        if (!TryGetComponent<MeshCollider>(out meshCollider))
        {
            meshCollider = transform.AddComponent<MeshCollider>();
        }
    }
    public void SetMaterial(Material material)
    {
        if(Initialized)
            meshRenderer.sharedMaterial = material;
    }
    public void InitializeChunk()
    {
        if (mesh != null)
            Destroy(mesh);
        if(meshRenderer != null)
            meshRenderer.sharedMaterial = ChunkFactory.Instance.Material;
        
        if(meshData != null) meshData.ClearData();
        else meshData = new MeshData();

        Initialized = true;
    }
    #endregion

    #region Chunk Updates

    public void UploadMeshBuffer(ref ChunkMeshBuffer meshBuffer)
    {
        if (!Initialized)
        {
            InitializeComponents();
            InitializeChunk();
        }

        if(meshData == null)
            meshData = new MeshData();
        meshData.Initialize();

        int[] faceCount = new int[2] { 0, 0 };
        meshBuffer.countBuffer.GetData(faceCount);

        //Get all of the meshData from the buffers to local arrays
        meshBuffer.vertexBuffer.GetData(meshData.verts, 0, 0, faceCount[0]);
        meshBuffer.indexBuffer.GetData(meshData.indices, 0, 0, faceCount[1]);
        meshBuffer.normalBuffer.GetData(meshData.normals, 0, 0, faceCount[0]);
        meshBuffer.uvBuffer.GetData(meshData.uvs, 0, 0, faceCount[0]);
        meshBuffer.colorBuffer.GetData(meshData.colors, 0, 0, faceCount[0]);

        var mesh = new Mesh() { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

        mesh.SetVertices(meshData.verts, 0, faceCount[0]);
        mesh.SetIndices(meshData.indices, 0, faceCount[1], MeshTopology.Triangles, 0);
        mesh.SetUVs(0, meshData.uvs, 0, faceCount[0]);
        mesh.SetColors(meshData.colors, 0, faceCount[0]);
        mesh.SetNormals(meshData.normals, 0, faceCount[0]);

        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.Optimize();
        //mesh.UploadMeshData(false);
        UploadMesh(ref mesh);

        meshData.ClearData();
    }

    public void UploadMesh(ref Mesh mesh)
    {
        if (mesh != null)
        {
            if (this.mesh != null)
            {
                meshCollider.sharedMesh = null;
                meshFilter.sharedMesh = null;
                Destroy(this.mesh);
            }
            //Debug.Log("The new mesh has " + mesh.vertexCount + " vertices and " + mesh.GetIndices(0).Length + " triangles.");
            this.mesh = mesh;
            meshFilter.sharedMesh = this.mesh;
            meshCollider.sharedMesh = this.mesh;
            meshRenderer.sharedMaterial = ChunkFactory.Instance.Material;
        }
        else
        {
            //Debug.Log("New mesh is null");
        }
    }

    #endregion

    #region Mesh Data

    public (int,int) GetMeshVerticesAndTrianglesCount()
    {
        if (meshFilter != null && meshFilter.sharedMesh != null)
            return (meshFilter.sharedMesh.vertexCount, (int)meshFilter.sharedMesh.GetIndexCount(0));
        return (0,0);
    }

    [System.Serializable]
    public class MeshData
    {

        public int[] indices;
        public Vector3[] verts;
        public Vector3[] normals;
        public Vector2[] uvs;
        public Color[] colors;

        public bool initialized = false;

        public void Initialize()
        {
            int maxTris = WorldSettings.RenderedVoxelsPerChunk;
            indices = new int[maxTris * 6 * 6];
            verts = new Vector3[maxTris * 6 * 4];
            normals = new Vector3[maxTris * 6 * 4];
            uvs = new Vector2[maxTris * 6 * 4];
            colors = new Color[maxTris * 6 * 4];

            initialized = true;
        }

        public void ClearData()
        {
            indices = null;
            colors = null;
            verts = null; 
            normals = null;
            uvs = null;

            initialized = false;
        }
    }
    #endregion

    #region Clear Chunk
    public void ClearChunk()
    {
        meshCollider.sharedMesh = null;
        meshFilter.sharedMesh = null;

        if (mesh != null)
            Destroy(mesh);

        meshData?.ClearData();

        Initialized = false;
    }

    public void Dispose()
    {
        ClearChunk();
        if (meshFilter != null)
            GameObject.Destroy(meshFilter);
        if (meshRenderer != null)
            GameObject.Destroy(meshRenderer);
        if (meshCollider != null)
            GameObject.Destroy(meshCollider);
    }

    public void OnDestroy()
    {
        Dispose();
    }

    public void OnApplicationQuit()
    {
        Dispose();
    }
    #endregion
}
