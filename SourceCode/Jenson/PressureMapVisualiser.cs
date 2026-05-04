using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PressureMapVisualiser : MonoBehaviour
{
    [Header("Data Source")]
    public string pressureMapFolder = "PressureMaps";

    [Header("Playback")]
    public float animationSpeed = 1f;

    [Header("Plane Settings")]
    public Vector2 planeSize = new Vector2(10, 10);

    [Header("Colour Gradient")]
    public Gradient pressureGradient;

    [Header("Scene Alignment")]
    public Vector3 scenePosition = new Vector3(7.5f, 0f, 0f);

    private Mesh mesh;
    private Color[] colours;
    private List<Texture2D> pressureMaps = new List<Texture2D>();

    // Start at 1 to skip frame 0 which can have CFD startup interference
    private float timeAccumulator = 1f;

    void Start()
    {
        transform.position = scenePosition;

        mesh = GetComponent<MeshFilter>().mesh;
        colours = new Color[mesh.vertexCount];

        LoadPressureMaps();
    }

    void Update()
    {
        if (pressureMaps.Count == 0) return;

        timeAccumulator += Time.deltaTime * animationSpeed;

        float frameFloat = timeAccumulator % pressureMaps.Count;
        int frameA = Mathf.FloorToInt(frameFloat);
        int frameB = (frameA + 1) % pressureMaps.Count;
        float lerpT = frameFloat - frameA;

        ApplyPressureMaps(pressureMaps[frameA], pressureMaps[frameB], lerpT);
    }

    void LoadPressureMaps()
    {
        pressureMaps.Clear();

        string folderPath = Path.Combine(Application.streamingAssetsPath, pressureMapFolder);

        int i = 0;
        while (true)
        {
            string path = Path.Combine(folderPath, $"pressuremap_{i}.png");
            if (!File.Exists(path)) break;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(bytes);
            tex.Apply();
            pressureMaps.Add(tex);
            i++;
        }

        if (pressureMaps.Count == 0)
            Debug.LogError("PressureMapVisualiser: no pressure maps found — check StreamingAssets/" + pressureMapFolder + "/");
    }

    void ApplyPressureMaps(Texture2D mapA, Texture2D mapB, float t)
    {
        int w = mapA.width;
        int h = mapA.height;

        Vector3[] verts = mesh.vertices;

        for (int i = 0; i < verts.Length; i++)
        {
            float u = Mathf.InverseLerp(-planeSize.x * 0.5f, planeSize.x * 0.5f, verts[i].x);
            float v = Mathf.InverseLerp(-planeSize.y * 0.5f, planeSize.y * 0.5f, verts[i].z);

            int x = Mathf.Clamp(Mathf.RoundToInt(u * (w - 1)), 0, w - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (h - 1)), 0, h - 1);

            float pA = mapA.GetPixel(x, y).grayscale;
            float pB = mapB.GetPixel(x, y).grayscale;

            colours[i] = pressureGradient.Evaluate(Mathf.Lerp(pA, pB, t));
        }

        mesh.colors = colours;
    }
}