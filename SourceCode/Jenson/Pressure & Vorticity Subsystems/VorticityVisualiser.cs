using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Attach this component to the same GameObject as WaterSurface.
// It loads greyscale vorticity maps from StreamingAssets/VorticityMaps/ and
// animates them as a transparent colour overlay on the water surface mesh.
// Requires VorticityOverlay.shader to be present in Assets/Shaders/.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VorticityVisualiser : MonoBehaviour
{
    [Header("Data Source")]
    // Name of the folder inside StreamingAssets containing vorticitymap_0.png, vorticitymap_1.png ...
    public string vorticityMapFolder = "VorticityMaps";

    [Header("Playback")]
    // Match this to the animationSpeed value on the WaterSurface component
    public float animationSpeed = 1f;

    [Header("Scene Alignment")]
    // Match this to the position of the WaterSurface GameObject in the scene
    public Vector3 scenePosition = new Vector3(7.5f, 0f, 0f);

    private Material material;
    private List<Texture2D> vorticityMaps = new List<Texture2D>();
    private bool materialReady = false;

    // Start at 1 to skip frame 0 which can have CFD startup interference
    private float timeAccumulator = 1f;

    void Start()
    {
        transform.position = scenePosition;
        LoadVorticityMaps();
        StartCoroutine(InitMaterial());
    }

    IEnumerator InitMaterial()
    {
        // Wait for WaterSurface.Start() and PressureMapVisualiser.Start() to run first
        yield return null;
        yield return null;
        yield return null;

        // Append the vorticity overlay material after pressure has already appended
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Material[] mats = mr.materials;
        Material[] newMats = new Material[mats.Length + 1];
        for (int i = 0; i < mats.Length; i++)
            newMats[i] = mats[i];
        newMats[newMats.Length - 1] = new Material(Shader.Find("Custom/VorticityOverlay"));
        mr.materials = newMats;

        material = newMats[newMats.Length - 1];
        // Alpha controls overlay transparency — 0 is invisible, 1 is fully opaque
        material.SetFloat("_Alpha", 0.6f);
    }

    void Update()
    {
        if (vorticityMaps.Count == 0) return;

        // Search by shader name until material is confirmed — then cache and stop searching
        if (!materialReady)
        {
            foreach (Material m in GetComponent<MeshRenderer>().materials)
            {
                if (m != null && m.shader.name == "Custom/VorticityOverlay")
                {
                    material = m;
                    materialReady = true;
                    break;
                }
            }
            if (!materialReady) return;
        }

        timeAccumulator += Time.deltaTime * animationSpeed;

        // Interpolate between two adjacent vorticity map frames each frame
        float frameFloat = timeAccumulator % vorticityMaps.Count;
        int frameA = Mathf.FloorToInt(frameFloat);
        int frameB = (frameA + 1) % vorticityMaps.Count;
        float lerpT = frameFloat - frameA;

        // Pass both frames and blend factor to the shader
        material.SetTexture("_VorticityMapA", vorticityMaps[frameA]);
        material.SetTexture("_VorticityMapB", vorticityMaps[frameB]);
        material.SetFloat("_Blend", lerpT);
    }

    void LoadVorticityMaps()
    {
        vorticityMaps.Clear();

        string folderPath = Path.Combine(Application.streamingAssetsPath, vorticityMapFolder);

        // Load vorticitymap_0.png, vorticitymap_1.png ... until no more files are found
        int i = 0;
        while (true)
        {
            string path = Path.Combine(folderPath, $"vorticitymap_{i}.png");
            if (!File.Exists(path)) break;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(bytes);
            tex.Apply();
            vorticityMaps.Add(tex);
            i++;
        }

        if (vorticityMaps.Count == 0)
            Debug.LogError("VorticityVisualiser: no vorticity maps found — check StreamingAssets/" + vorticityMapFolder + "/");
    }
}