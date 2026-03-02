using UnityEngine;
using System.Collections.Generic;

public class SimpleBladeSpinner : MonoBehaviour
{
    public TextAsset csvFile;
    public Transform blades;
    public float tipSpeedRatio = 7.0f;

    [Header("Rotation Settings")]
    public bool reverseRotation = false;
    public enum Axis { X, Y, Z }
    public Axis rotationAxis = Axis.Z; // Change this in the Inspector!

    private List<float> windSpeeds = new List<float>();
    private float timer = 0f;

    void Start() {
        if (!csvFile) {
            Debug.LogError("CSV File missing on " + gameObject.name);
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++) {
            string line = lines[i].Trim();
            if (!string.IsNullOrWhiteSpace(line)) {
                string[] values = line.Split(',');
                if(values.Length > 1)
                    windSpeeds.Add(float.Parse(values[1]));
            }
        }
    }

    void Update() {
        if (windSpeeds.Count < 2 || blades == null) return;

        // 1. Calculate the current wind speed from data
        timer = (timer + Time.deltaTime) % (windSpeeds.Count - 1);
        int idx = Mathf.FloorToInt(timer);
        float speed = Mathf.Lerp(windSpeeds[idx], windSpeeds[idx + 1], timer % 1);

        // 2. Determine the Axis Vector
        Vector3 chosenAxis = Vector3.forward;
        if (rotationAxis == Axis.X) chosenAxis = Vector3.right;
        else if (rotationAxis == Axis.Y) chosenAxis = Vector3.up;

        // 3. Apply rotation relative to ITSELF (Space.Self)
        float direction = reverseRotation ? -1f : 1f;
        float rotationAmount = speed * tipSpeedRatio * Time.deltaTime * 50f * direction;

        blades.Rotate(chosenAxis, rotationAmount, Space.Self);
    }
}
