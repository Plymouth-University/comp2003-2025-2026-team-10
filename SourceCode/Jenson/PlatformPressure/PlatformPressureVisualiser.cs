using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Attach this component to the NewFloater GameObject on the turbine.
// It loads greyscale platform pressure maps from StreamingAssets/PlatformPressureMaps/
// and animates them as a transparent blue-to-red colour overlay on the platform mesh.
// Requires PressureOverlay.shader to be present in Assets/Shaders/.
[RequireComponent(typeof(MeshRenderer))]
public class PlatformPressureVisualiser : MonoBehaviour
{
    [Header("Data Source")]
    // Name of the folder inside StreamingAssets containing platformpressure_0.png, platformpressure_1.png ...
    public string pressureMapFolder = "PlatformPressureMaps";

    [Header("Playback")]
    public float animationSpeed = 1f;

    private Material material;
    private List<Texture2D> pressureMaps = new List<Texture2D>();
    private bool materialReady = false;

    // Start at 1 to skip frame 0 which can have CFD startup interference
    private float timeAccumulator = 1f;

    void Start()
    {
        LoadPressureMaps();
        StartCoroutine(InitMaterial());
    }

    IEnumerator InitMaterial()
    {
        // Wait for any other Start() calls on this GameObject to complete
        yield return null;
        yield return null;

        // Append pressure overlay material to the platform's existing material
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
    }

    void Update()
    {
        if (pressureMaps.Count == 0) return;

        // Search by shader name until material is confirmed — then cache and stop searching
        if (!materialReady)
        {
            foreach (Material m in GetComponent<MeshRenderer>().materials)
            {
                if (m != null && m.shader.name == "Custom/PressureOverlay")
                {
                    material = m;
                    materialReady = true;
                    break;
                }
            }
            if (!materialReady) return;
        }

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

        // Load platformpressure_0.png, platformpressure_1.png ... until no more files are found
        int i = 0;
        while (true)
        {
            string path = Path.Combine(folderPath, $"platformpressure_{i}.png");
            if (!File.Exists(path)) break;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(bytes);
            tex.Apply();
            pressureMaps.Add(tex);
            i++;
        }

        if (pressureMaps.Count == 0)
            Debug.LogError("PlatformPressureVisualiser: no pressure maps found — check StreamingAssets/" + pressureMapFolder + "/");
    }
}
