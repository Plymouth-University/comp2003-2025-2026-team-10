using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterSurface : MonoBehaviour
{
    [Header("References")]
    public WaveDataLoader loader;
    public Material waterMaterial;

    [Header("Mesh Settings")]
    public int meshResolution = 256;
    public float meshSize = 20f;
    public float heightScale = 3f;

    [Header("Playback")]
    public float animationSpeed = 8f;

    private Mesh mesh;
    private Vector3[] vertices;
    private float playbackTime;
    private RenderTexture interpolatedRT;
    private Texture2D readbackTex;
    private bool waterMaterialPinned = false;

    void Start()
    {
        BuildMesh();
        GetComponent<MeshRenderer>().material = waterMaterial;

        // Create a RenderTexture to receive the interpolated shader output
        interpolatedRT = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
        interpolatedRT.Create();

        readbackTex = new Texture2D(512, 512, TextureFormat.ARGB32, false);
    }

    void BuildMesh()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int vertCount = (meshResolution + 1) * (meshResolution + 1);
        vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[meshResolution * meshResolution * 6];

        for (int y = 0; y <= meshResolution; y++)
            for (int x = 0; x <= meshResolution; x++)
            {
                int idx = y * (meshResolution + 1) + x;
                float u = (float)x / meshResolution;
                float v = (float)y / meshResolution;
                vertices[idx] = new Vector3((u - 0.5f) * meshSize, 0f, (v - 0.5f) * meshSize);
                uvs[idx] = new Vector2(u, v);
            }

            int t = 0;
        for (int y = 0; y < meshResolution; y++)
            for (int x = 0; x < meshResolution; x++)
            {
                int bl = y * (meshResolution + 1) + x;
                int br = bl + 1;
                int tl = bl + meshResolution + 1;
                int tr = tl + 1;
                tris[t++] = bl; tris[t++] = tl; tris[t++] = tr;
                tris[t++] = bl; tris[t++] = tr; tris[t++] = br;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
        if (!loader.isLoaded) return;

        // Re-pin waterMaterial to Element 0 only until confirmed stable —
        // overlay coroutines rebuilding the materials array can displace it. - jenson
        if (!waterMaterialPinned)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr.materials[0] != waterMaterial)
            {
                Material[] mats = mr.materials;
                mats[0] = waterMaterial;
                mr.materials = mats;
            }
            else
            {
                waterMaterialPinned = true;
            }
        }

        playbackTime = (playbackTime + Time.deltaTime * animationSpeed) % loader.frameCount;

        int   frameA = Mathf.FloorToInt(playbackTime);
        int   frameB = (frameA + 1) % loader.frameCount;
        float blend  = playbackTime - frameA;

        waterMaterial.SetTexture("_HeightmapA", loader.heightmaps[frameA]);
        waterMaterial.SetTexture("_HeightmapB", loader.heightmaps[frameB]);
        waterMaterial.SetTexture("_FlowMap",    loader.flowMaps[Mathf.Min(frameA, loader.flowMaps.Length - 1)]);
        waterMaterial.SetFloat("_Blend",        blend);

        DisplaceVertices(loader.heightmaps[frameA], loader.heightmaps[frameB], blend);
    }

    void DisplaceVertices(Texture2D hmA, Texture2D hmB, float blend)
    {
        if (hmA == null || hmB == null) return;

        for (int y = 0; y <= meshResolution; y++)
            for (int x = 0; x <= meshResolution; x++)
            {
                int   idx    = y * (meshResolution + 1) + x;
                float u      = (float)x / meshResolution;
                float v      = (float)y / meshResolution;

                float hA     = hmA.GetPixelBilinear(u, v).r;
                float hB     = hmB.GetPixelBilinear(u, v).r;
                float height = Mathf.Lerp(hA, hB, blend);

                vertices[idx].y = (height - 0.5f) * heightScale;
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
    }

    public float GetHeightAtUV(Vector2 uv)
    {
        if (!loader.isLoaded) return 0.5f;

        int frameA = Mathf.FloorToInt(playbackTime);
        int frameB = (frameA + 1) % loader.frameCount;
        float blend = playbackTime - frameA;

        float hA = loader.heightmaps[frameA].GetPixelBilinear(uv.x, uv.y).r;
        float hB = loader.heightmaps[frameB].GetPixelBilinear(uv.x, uv.y).r;

        return Mathf.Lerp(hA, hB, blend);
    }

    void OnDestroy()
    {
        if (interpolatedRT != null) interpolatedRT.Release();
    }
}
