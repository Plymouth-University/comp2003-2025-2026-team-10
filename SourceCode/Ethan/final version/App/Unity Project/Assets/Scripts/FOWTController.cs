using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FOWTController : MonoBehaviour
{
    public Transform platformTransform;
    public Transform bladeTransform;
    public float tipSpeedRatio = 7.0f;
    public float playbackSpeed = 1.0f;

    [Header("Scene Alignment")]
    public Vector3 scenePosition = new Vector3(7.5f, 2.135f, 0f);

    [Header("Bobbing")]
    public float bobbingOffset = 0f;

    private struct Snapshot
    {
        public float time, windSpeed, pitch;
        public Vector3 pos;
    }

    private List<Snapshot> snaps = new List<Snapshot>();
    private float timer = 0f;

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Turbine", "turbine.csv");

        if (!File.Exists(path))
        {
            Debug.LogError("FOWTController: turbine.csv not found at " + path);
            return;
        }

        transform.position = scenePosition;
        Parse(File.ReadAllText(path));
    }

    void Update()
    {
        if (snaps.Count < 2) return;

        timer = (timer + Time.deltaTime * playbackSpeed) % snaps[snaps.Count - 1].time;

        int i = 0;
        while (i < snaps.Count - 2 && timer > snaps[i + 1].time) i++;

        float t = (timer - snaps[i].time) / (snaps[i + 1].time - snaps[i].time);

        Vector3 pos = Vector3.Lerp(snaps[i].pos, snaps[i + 1].pos, t);
        pos.y += bobbingOffset;
        platformTransform.localPosition = pos;
        platformTransform.localRotation = Quaternion.Euler(
            Mathf.Lerp(snaps[i].pitch, snaps[i + 1].pitch, t), 0, 0);

        float speed = Mathf.Lerp(snaps[i].windSpeed, snaps[i + 1].windSpeed, t);
        bladeTransform.Rotate(Vector3.forward, speed * tipSpeedRatio * Time.deltaTime * 50f);
    }

    void Parse(string csvText)
    {
        string[] lines = csvText.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] c = lines[i].Split(',');

            snaps.Add(new Snapshot
            {
                time = float.Parse(c[0]),
                windSpeed = float.Parse(c[1]),
                pos = new Vector3(0, float.Parse(c[3]), float.Parse(c[2])), // Heave_Z→Y, Surge_X→Z
                pitch = float.Parse(c[4])
            });
        }
    }
}
