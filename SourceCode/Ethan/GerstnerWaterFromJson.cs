using System;
using System.IO;
using UnityEngine;

[Serializable]
public class WaveComponent
{
    public float dir_x;
    public float dir_z;
    public float amplitude;
    public float wavelength;
    public float phase;
    public float speed;      // phase speed (m/s)
    public float steepness;  // 0..1
}

[Serializable]
public class WaveJson
{
    public string model;
    public float gravity;
    public WaveComponent[] waves;
}

[RequireComponent(typeof(MeshFilter))]
public class GerstnerWaterFromJson : MonoBehaviour
{
    public string jsonFileName = "waves.json";
    public float timeScale = 1f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] deformedVertices;

    private WaveJson data;

    void Start()
    {
        var mf = GetComponent<MeshFilter>();
        mesh = Instantiate(mf.sharedMesh);
        mf.sharedMesh = mesh;

        baseVertices = mesh.vertices;
        deformedVertices = new Vector3[baseVertices.Length];

        LoadJson();
    }

    void Update()
    {
        if (data == null || data.waves == null || data.waves.Length == 0) return;

        float t = Time.time * timeScale;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            deformedVertices[i] = ApplyGerstner(baseVertices[i], t);
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
    }

    void LoadJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"waves.json not found at: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        data = JsonUtility.FromJson<WaveJson>(json);

        if (data == null || data.waves == null || data.waves.Length == 0)
        {
            Debug.LogError("Parsed JSON but found no waves. Expected fields: gravity, waves[].dir_x, dir_z, amplitude, wavelength, phase, speed, steepness.");
            return;
        }

        if (data.gravity <= 0f) data.gravity = 9.81f;

        Debug.Log($"Loaded {data.waves.Length} wave components. model={data.model}, gravity={data.gravity}");
    }

    Vector3 ApplyGerstner(Vector3 localPos, float t)
    {
        Vector3 pW = transform.TransformPoint(localPos);

        float dx = 0f, dz = 0f, dy = 0f;

        float g = data.gravity;

        foreach (var w in data.waves)
        {
            float A = w.amplitude;
            float L = Mathf.Max(0.0001f, w.wavelength);

            // wave number k = 2π / L
            float k = (2f * Mathf.PI) / L;

            // direction (normalized)
            Vector2 D = new Vector2(w.dir_x, w.dir_z);
            if (D.sqrMagnitude < 1e-8f) D = Vector2.right;
            D.Normalize();

            // If speed provided, omega = k * c. Otherwise deep-water omega = sqrt(gk)
            float c = w.speed;
            float omega = (c > 0f) ? (k * c) : Mathf.Sqrt(g * k);

            float theta = k * (D.x * pW.x + D.y * pW.z) + omega * t + w.phase;

            float Q = Mathf.Clamp01(w.steepness);

            dx += Q * A * D.x * Mathf.Cos(theta);
            dz += Q * A * D.y * Mathf.Cos(theta);
            dy += A * Mathf.Sin(theta);
        }

        Vector3 displacedW = new Vector3(pW.x + dx, pW.y + dy, pW.z + dz);
        return transform.InverseTransformPoint(displacedW);
    }
}
