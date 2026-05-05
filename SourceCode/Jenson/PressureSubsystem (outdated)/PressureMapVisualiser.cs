using System.Collections.Generic;
using System.IO;
using UnityEngine;
// how to put it in updated app versions below
// Attach this component to the same GameObject as WaterSurface.
// It loads greyscale pressure maps from StreamingAssets/PressureMaps/ and
// animates them as a transparent colour overlay on the water surface mesh.
// Requires PressureOverlay.shader to be present in Assets/Shaders/.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PressureMapVisualiser : MonoBehaviour
{
    [Header("Data Source")]
    // Name of the folder inside StreamingAssets containing pressuremap_0.png, pressuremap_1.png ...
    public string pressureMapFolder = "PressureMaps";

    [Header("Playback")]
    // Match this to the animationSpeed value on the WaterSurface component
    public float animationSpeed = 1f;

    [Header("Scene Alignment")]
    // Match this to the position of the WaterSurface GameObject in the scene
    public Vector3 scenePosition = new Vector3(7.5f, 0f, 0f);

    private Material material;
    private List<Texture2D> pressureMaps = new List<Texture2D>();

    // Start at 1 to skip frame 0 which can have CFD startup interference
    private float timeAccumulator = 1f;

    void Start()
    {
        transform.position = scenePosition;

        // Append the pressure overlay material to whatever materials are already
        // on the MeshRenderer — this preserves the existing water material in
        // Element 0 and adds the pressure overlay as Element 1.
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Material[] mats = mr.materials;
        Material[] newMats = new Material[mats.Length + 1];
        for (int i = 0; i < mats.Length; i++)
            newMats[i] = mats[i];
        newMats[newMats.Length - 1] = new Material(Shader.Find("Custom/PressureOverlay"));
        mr.materials = newMats;

        material = newMats[newMats.Length - 1];
        // Alpha controls overlay transparency — 0 is invisible, 1 is fully opaque
        material.SetFloat("_Alpha", 0.6f);

        LoadPressureMaps();
    }

    void Update()
    {
        if (pressureMaps.Count == 0) return;

        timeAccumulator += Time.deltaTime * animationSpeed;

        // Interpolate between two adjacent pressure map frames each frame
        float frameFloat = timeAccumulator % pressureMaps.Count;
        int frameA = Mathf.FloorToInt(frameFloat);
        int frameB = (frameA + 1) % pressureMaps.Count;
        float lerpT = frameFloat - frameA;

        // Pass both frames and blend factor to the shader
        material.SetTexture("_PressureMapA", pressureMaps[frameA]);
        material.SetTexture("_PressureMapB", pressureMaps[frameB]);
        material.SetFloat("_Blend", lerpT);
    }

    void LoadPressureMaps()
    {
        pressureMaps.Clear();

        string folderPath = Path.Combine(Application.streamingAssetsPath, pressureMapFolder);

        // Load pressuremap_0.png, pressuremap_1.png ... until no more files are found
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
