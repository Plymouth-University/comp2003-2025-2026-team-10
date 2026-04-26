using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HeightMapWaterPlane : MonoBehaviour
{
    [Header("Plane Settings")]
    public Vector2 planeSize = new Vector2(10, 10);
    [Range(1, 200)]
    public int planeResolution = 100;

    [Header("Height Map Settings")]
    public string heightMapFolder = "HeightMaps";
    public float animationSpeed = 1f;
    public float heightScale = 2f;

    [Header("Scene Alignment")]
    public Vector3 scenePosition = new Vector3(7.5f, 0f, 0f);

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] deformedVertices;
    private List<Texture2D> heightMaps = new List<Texture2D>();

    // Start at 1 to skip frame 0 which can have CFD startup interference
    private float timeAccumulator = 1f;

    void Start()
    {
        transform.position = scenePosition;
        GeneratePlane();
        LoadHeightMaps();
    }

    void Update()
    {
        if (heightMaps.Count == 0) return;

        timeAccumulator += Time.deltaTime * animationSpeed;

        float frameFloat = timeAccumulator % heightMaps.Count;
        int frameA = Mathf.FloorToInt(frameFloat);
        int frameB = (frameA + 1) % heightMaps.Count;
        float lerpT = frameFloat - frameA;

        ApplyHeightMaps(heightMaps[frameA], heightMaps[frameB], lerpT);
    }

    void GeneratePlane()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float stepX = planeSize.x / planeResolution;
        float stepZ = planeSize.y / planeResolution;

        for (int z = 0; z <= planeResolution; z++)
        {
            for (int x = 0; x <= planeResolution; x++)
            {
                vertices.Add(new Vector3(
                    x * stepX - planeSize.x * 0.5f,
                    0f,
                    z * stepZ - planeSize.y * 0.5f
                ));
            }
        }

        for (int z = 0; z < planeResolution; z++)
        {
            for (int x = 0; x < planeResolution; x++)
            {
                int i = z * (planeResolution + 1) + x;

                triangles.Add(i);
                triangles.Add(i + planeResolution + 1);
                triangles.Add(i + planeResolution + 2);

                triangles.Add(i);
                triangles.Add(i + planeResolution + 2);
                triangles.Add(i + 1);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        baseVertices = mesh.vertices;
        deformedVertices = new Vector3[baseVertices.Length];
    }

    void LoadHeightMaps()
    {
        heightMaps.Clear();

        string folderPath = Path.Combine(Application.streamingAssetsPath, heightMapFolder);

        int i = 0;
        while (true)
        {
            string path = Path.Combine(folderPath, $"heightmap_{i}.png");
            if (!File.Exists(path)) break;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(bytes);
            tex.Apply();
            heightMaps.Add(tex);
            i++;
        }

        if (heightMaps.Count == 0)
            Debug.LogError("HeightMapWaterPlane: no heightmaps loaded — check StreamingAssets/HeightMaps/");
    }

    void ApplyHeightMaps(Texture2D mapA, Texture2D mapB, float t)
    {
        int w = mapA.width;
        int h = mapA.height;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 v = baseVertices[i];

            float u = Mathf.InverseLerp(-planeSize.x * 0.5f, planeSize.x * 0.5f, v.x);
            float vCoord = Mathf.InverseLerp(-planeSize.y * 0.5f, planeSize.y * 0.5f, v.z);

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (w - 1)), 0, w - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(vCoord * (h - 1)), 0, h - 1);

            float hA = mapA.GetPixel(x, y).grayscale;
            float hB = mapB.GetPixel(x, y).grayscale;

            float height = Mathf.Lerp(hA, hB, t) * heightScale;

            deformedVertices[i] = new Vector3(v.x, height, v.z);
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
    }
}
