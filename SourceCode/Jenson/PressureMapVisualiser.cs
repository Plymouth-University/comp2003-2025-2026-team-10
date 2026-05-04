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

    [Header("Scene Alignment")]
    public Vector3 scenePosition = new Vector3(7.5f, 0f, 0f);

    private Material material;
    private List<Texture2D> pressureMaps = new List<Texture2D>();

    // Start at 1 to skip frame 0 which can have CFD startup interference
    private float timeAccumulator = 1f;

    void Start()
    {
        transform.position = scenePosition;
        material = GetComponent<MeshRenderer>().material;
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

        material.SetTexture("_PressureMapA", pressureMaps[frameA]);
        material.SetTexture("_PressureMapB", pressureMaps[frameB]);
        material.SetFloat("_Blend", lerpT);
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
}
