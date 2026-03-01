using UnityEngine;
using UnityEngine.Rendering;

public class FluidMorphController : MonoBehaviour
{
    [Header("SDF Frames")]
    public Texture3D[] sdfFrames;
    public int frameA = 0;
    public int frameB = 1;

    [Range(0, 1)]
    public float morph = 0f;

    public ComputeShader sdfShader;
    public ComputeShader marchingShader;

    public MeshFilter outputMeshFilter;

    const int RES = 128;
    const int MAX_TRIS = 5_000_000;

    RenderTexture sdfA;
    RenderTexture sdfB;
    RenderTexture sdfResult;

    ComputeBuffer triBuffer;
    ComputeBuffer triCountBuffer;

    struct VertexData
    {
        public Vector3 posA;
        public Vector3 posB;
        public Vector3 posC;
    }

    bool initialized = false;

    void Start()
    {
        Debug.Log("SDF shader: " + sdfShader);
        Debug.Log("March shader: " + marchingShader);

        if (sdfShader == null || marchingShader == null)
        {
            Debug.LogError("Missing compute shaders!");
            return;
        }

        sdfA = Create3DTexture();
        sdfB = Create3DTexture();
        sdfResult = Create3DTexture();

        triBuffer = new ComputeBuffer(
            MAX_TRIS,
            sizeof(float) * 9,
            ComputeBufferType.Append);

        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        initialized = true;
    }

    RenderTexture Create3DTexture()
    {
        var rt = new RenderTexture(RES, RES, 0, RenderTextureFormat.RFloat);
        rt.dimension = TextureDimension.Tex3D;
        rt.volumeDepth = RES;
        rt.enableRandomWrite = true;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
        return rt;
    }

    void Update()
    {
        if (!initialized) return;

        if (sdfFrames == null || sdfFrames.Length == 0)
        {
            Debug.LogWarning("No SDF frames assigned");
            return;
        }

        if (outputMeshFilter == null)
        {
            Debug.LogWarning("Output MeshFilter missing");
            return;
        }

        if (frameA >= sdfFrames.Length || frameB >= sdfFrames.Length)
        {
            Debug.LogWarning("Frame index out of range");
            return;
        }

        //copy frames to GPU
        if (sdfFrames != null && sdfFrames.Length > 1)
        {
            Graphics.CopyTexture(sdfFrames[frameA], sdfA);
            Graphics.CopyTexture(sdfFrames[frameB], sdfB);
        }

        //sdf lerp
        int k = sdfShader.FindKernel("LerpSDF");

        sdfShader.SetFloat("_T", morph);
        sdfShader.SetTexture(k, "_SdfA", sdfA);
        sdfShader.SetTexture(k, "_SdfB", sdfB);
        sdfShader.SetTexture(k, "_Result", sdfResult);

        sdfShader.Dispatch(k, RES / 8, RES / 8, RES / 8);

        //marching cubes
        int mk = marchingShader.FindKernel("March");

        triBuffer.SetCounterValue(0);

        marchingShader.SetTexture(mk, "_SDF", sdfResult);
        marchingShader.SetBuffer(mk, "_Triangles", triBuffer);
        marchingShader.SetInt("_Resolution", RES);
        marchingShader.SetFloat("_IsoLevel", 0.5f);

        marchingShader.Dispatch(mk, RES / 8, RES / 8, RES / 8);

        //read back triangle count
        ComputeBuffer.CopyCount(triBuffer, triCountBuffer, 0);

        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int triCount = triCountArray[0];

        Debug.Log("TriCount = " + triCount);

        if (triCount <= 0)
        {
            outputMeshFilter.sharedMesh = null;
            return;
        }

        VertexData[] tris = new VertexData[triCount];
        triBuffer.GetData(tris, 0, 0, triCount);

        BuildMesh(tris, triCount);
    }

    void BuildMesh(VertexData[] tris, int triCount)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;

        Vector3[] verts = new Vector3[triCount * 3];
        int[] indices = new int[triCount * 3];

        for (int i = 0; i < triCount; i++)
        {
            verts[i * 3 + 0] = tris[i].posA;
            verts[i * 3 + 1] = tris[i].posB;
            verts[i * 3 + 2] = tris[i].posC;

            indices[i * 3 + 0] = i * 3 + 0;
            indices[i * 3 + 1] = i * 3 + 1;
            indices[i * 3 + 2] = i * 3 + 2;
        }

        mesh.vertices = verts;
        mesh.triangles = indices;
        mesh.RecalculateNormals();

        outputMeshFilter.sharedMesh = mesh;
    }

    void OnDestroy()
    {
        triBuffer?.Release();
        triCountBuffer?.Release();
        sdfA?.Release();
        sdfB?.Release();
        sdfResult?.Release();
    }
}