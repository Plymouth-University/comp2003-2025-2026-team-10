using UnityEngine;
using System.Collections.Generic;

public class FOWTController : MonoBehaviour
{
    public enum RotationAxis { X, Y, Z }

    [Header("References")]
    private TextAsset csvFile;
    public Transform bladeTransform;

    [Header("Orientation")]
    public RotationAxis axisOfRotation = RotationAxis.Z;
    public bool invertRotation = false;

    [Header("Physical Properties")]
    public float tipSpeedRatio = 7.5f;
    public float rotorRadius = 1.96f;
    public float playbackSpeed = 1.0f;

    private struct Snapshot
    {
        public float time, windSpeed;
    }

    private List<Snapshot> snaps = new List<Snapshot>();
    private float timer = 0f;

    void Start()
    {
        csvFile = Resources.Load<TextAsset>("Unity_Wind_Data");
        if (csvFile)
        {
            Parse();
            Debug.Log($"FOWTController: Loaded {snaps.Count} snapshots.");
        }
        else
        {
            Debug.LogError("FOWTController: Could not find Unity_Wind_Data in Resources folder!");
        }
    }

    void Update()
    {
        if (snaps.Count < 2 || bladeTransform == null) return;

        timer = (timer + Time.deltaTime * playbackSpeed) % snaps[snaps.Count - 1].time;

        int i = 0;
        while (i < snaps.Count - 2 && timer > snaps[i + 1].time) i++;

        float t = (timer - snaps[i].time) / (snaps[i + 1].time - snaps[i].time);
        float currentWindSpeed = Mathf.Lerp(snaps[i].windSpeed, snaps[i + 1].windSpeed, t);

        float angularVelocityRad = (currentWindSpeed * tipSpeedRatio) / rotorRadius;
        float degreesPerSecond   = angularVelocityRad * Mathf.Rad2Deg;
        float rotationStep       = degreesPerSecond * Time.deltaTime * (invertRotation ? -1f : 1f);

        Vector3 chosenAxis = Vector3.forward;
        switch (axisOfRotation)
        {
            case RotationAxis.X: chosenAxis = Vector3.right;   break;
            case RotationAxis.Y: chosenAxis = Vector3.up;      break;
            case RotationAxis.Z: chosenAxis = Vector3.forward; break;
        }

        bladeTransform.Rotate(chosenAxis, rotationStep, Space.Self);
    }

    void Parse()
    {
        snaps.Clear();
        string[] lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] columns = lines[i].Split(',');
            if (columns.Length >= 2)
            {
                snaps.Add(new Snapshot {
                    time      = float.Parse(columns[0]),
                          windSpeed = float.Parse(columns[1])
                });
            }
        }
    }
}
