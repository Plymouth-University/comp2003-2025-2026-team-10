using UnityEngine;
using System.Collections.Generic;

public class FOWT_FullController : MonoBehaviour
{
    public TextAsset csvFile;
    [Header("Transforms")]
    public Transform bladeHub;     // The object that rotates
    public Transform platformRoot; // The parent object for Heave/Pitch

    [Header("Sensitivity")]
    public float tipSpeedRatio = 7.0f;
    public float heaveScale = 1.0f;
    public float pitchScale = 1.0f;

    private List<float> winds = new List<float>(), heaves = new List<float>(), pitches = new List<float>();
    private float timer = 0f;

    void Start() {
        if (csvFile == null) return;
        string[] lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++) {
            string[] cols = lines[i].Split(',');
            if (cols.Length >= 4) {
                winds.Add(float.Parse(cols[1]));
                heaves.Add(float.Parse(cols[2]));
                pitches.Add(float.Parse(cols[3]));
            }
        }
    }

    void Update() {
        if (winds.Count < 2) return;

        // Advance timer and loop
        timer = (timer + Time.deltaTime) % (winds.Count - 1);
        int idx = Mathf.FloorToInt(timer);
        float lerp = timer % 1.0f;

        // Interpolate data
        float w = Mathf.Lerp(winds[idx], winds[idx + 1], lerp);
        float h = Mathf.Lerp(heaves[idx], heaves[idx + 1], lerp);
        float p = Mathf.Lerp(pitches[idx], pitches[idx + 1], lerp);

        // 1. BLADE SPIN (Rotation speed based on wind)
        // Unity Forward is the spin axis
        float rotationSpeed = (w * tipSpeedRatio) * 50f; 
        bladeHub.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.Self);

        // 2. HEAVE (Up/Down)
        // Map OpenFOAM Z to Unity Y
        platformRoot.localPosition = new Vector3(0, h * heaveScale, 0);

        // 3. PITCH (Tilting)
        // Rotate around the Unity X axis (the "side-to-side" axis)
        platformRoot.localRotation = Quaternion.Euler(p * pitchScale, 0, 0);
    }
}
