using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class HeightMapWaterAnimator : MonoBehaviour
{
    public string heightMapFolder = "HeightMaps";   // EXACT folder name
    public int frameCount = 11;                     // heightmap_0.png ... heightmap_10.png
    public float animationSpeed = 1f;
    public float heightScale = 1f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] deformedVertices;

    private List<Texture2D> heightMaps = new List<Texture2D>();

    private float timeAccumulator = 0f;

    void Start()
    {
        LoadHeightMaps();

        mesh = Instantiate(GetComponent<MeshFilter>().sharedMesh);
        GetComponent<MeshFilter>().sharedMesh = mesh;

        baseVertices = mesh.vertices;
        deformedVertices = new Vector3[baseVertices.Length];
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

    void LoadHeightMaps()
    {
        heightMaps.Clear();

        string folderPath = Path.Combine(Application.streamingAssetsPath, heightMapFolder);

        for (int i = 0; i < frameCount; i++)
        {
            string path = Path.Combine(folderPath, $"heightmap_{i}.png");

            if (!File.Exists(path))
            {
                Debug.LogError("Missing heightmap: " + path);
                continue;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(bytes);
            tex.Apply();

            heightMaps.Add(tex);
        }

        Debug.Log("Loaded " + heightMaps.Count + " heightmaps");
    }

    void ApplyHeightMaps(Texture2D mapA, Texture2D mapB, float t)
    {
        int texWidth = mapA.width;
        int texHeight = mapA.height;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 v = baseVertices[i];

            float u = Mathf.InverseLerp(-0.5f, 0.5f, v.x);
            float vCoord = Mathf.InverseLerp(-0.5f, 0.5f, v.z);

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (texWidth - 1)), 0, texWidth - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(vCoord * (texHeight - 1)), 0, texHeight - 1);

            float hA = mapA.GetPixel(x, y).grayscale;
            float hB = mapB.GetPixel(x, y).grayscale;

            float height = Mathf.Lerp(hA, hB, t) * heightScale;

            deformedVertices[i] = new Vector3(v.x, height, v.z);
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
    }
}

