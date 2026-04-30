using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class CombinedStreamlineSystem : MonoBehaviour
{
    [Header("Rendering")]
    public GameObject particlePrefab;
    public Transform particleParent;

    [Header("Simulation")]
    public float speed = 3f;
    public int particlesPerStreamline = 1;

    [Header("File Control")]
    public float fileSwitchInterval = 5f;

    [Header("Scene Alignment")]
    public Vector3 sceneOffset = Vector3.zero;

    [Header("Trail")]
    public float trailDuration = 1.5f;
    public float trailWidth = 0.15f;

    [Header("Coloring")]
    public Gradient windGradient;

    float trailDisableTime = 0.5f;
    Material _trailMat;

    class Streamline
    {
        public List<Vector3> points;
        public List<float> distances;
        public float totalLength;
    }

    class StreamlineFile
    {
        public List<Streamline> streamlines = new();
    }

    class ParticleState
    {
        public Transform transform;
        public TrailRenderer trail;

        public int fileIndex;
        public int streamlineIndex;

        public float distanceAlong;
        public bool looping;
        public bool trailDisabled;
    }

    readonly List<StreamlineFile> files = new();
    readonly List<ParticleState> particles = new();

    int currentFile = 0;
    float fileTimer = 0f;

    void Start()
    {
        // Particles/Unlit reads vertex colors — required for trail startColor/endColor to work
        _trailMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        SetupGradient();
        LoadFiles();
        if (files.Count > 0)
            StartFile(0);
    }

    void OnDestroy()
    {
        if (_trailMat != null) Destroy(_trailMat);
    }

    void SetupGradient()
    {
        GradientColorKey[] colorKeys =
        {
            new GradientColorKey(new Color(0.05f, 0.3f, 1f),    0f),  // blue  — slow/wake
            new GradientColorKey(new Color(1f,    0.1f, 0.05f), 1f)   // red   — fast/freestream
        };
        GradientAlphaKey[] alphaKeys =
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };
        windGradient.SetKeys(colorKeys, alphaKeys);
    }

    void Update()
    {
        if (files.Count == 0) return;

        float dt = Time.deltaTime;

        UpdateParticles(dt);

        fileTimer += dt;

        if (fileTimer >= fileSwitchInterval)
        {
            fileTimer = 0f;

            foreach (var p in particles)
            {
                if (p.fileIndex == currentFile)
                    p.looping = false;
            }

            currentFile = (currentFile + 1) % files.Count;

            StartFile(currentFile);
        }
    }

    void LoadFiles()
    {
        TextAsset[] csvFiles = Resources.LoadAll<TextAsset>("Streamline");
        Quaternion rot = Quaternion.Euler(-90f, 0f, 0f);

        foreach (var csv in csvFiles)
        {
            var rawPaths = new Dictionary<int, List<Vector3>>();

            var lines = csv.text.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var p = lines[i].Split(',');

                int id = int.Parse(p[0]);

                float x = float.Parse(p[1], CultureInfo.InvariantCulture);
                float y = float.Parse(p[2], CultureInfo.InvariantCulture);
                float z = float.Parse(p[3], CultureInfo.InvariantCulture);

                Vector3 pos = rot * new Vector3(x, y, z);

                if (!rawPaths.TryGetValue(id, out var list))
                {
                    list = new List<Vector3>();
                    rawPaths[id] = list;
                }

                list.Add(pos);
            }

            int half = rawPaths.Count / 2;

            var file = new StreamlineFile();

            for (int i = 0; i < half; i++)
            {
                if (!rawPaths.ContainsKey(i) || !rawPaths.ContainsKey(i + half))
                    continue;

                List<Vector3> backward = rawPaths[i];
                List<Vector3> forward  = rawPaths[i + half];

                List<Vector3> combined = new List<Vector3>(backward.Count + forward.Count);

                for (int j = backward.Count - 1; j >= 0; j--)
                    combined.Add(backward[j]);

                for (int j = 0; j < forward.Count; j++)
                    combined.Add(forward[j]);

                if (combined.Count < 4) continue;

                file.streamlines.Add(BuildArcLengthStreamline(combined));
            }

            files.Add(file);
        }

        if (files.Count == 0)
            Debug.LogWarning("CombinedStreamlineSystem: no streamline CSVs found in Resources/Streamline/");
    }

    Streamline BuildArcLengthStreamline(List<Vector3> pts)
    {
        var s = new Streamline
        {
            points    = pts,
            distances = new List<float>(pts.Count)
        };

        float total = 0f;
        s.distances.Add(0f);

        for (int i = 1; i < pts.Count; i++)
        {
            total += Vector3.Distance(pts[i - 1], pts[i]);
            s.distances.Add(total);
        }

        s.totalLength = total;

        return s;
    }

    void StartFile(int fileIndex)
    {
        if (fileIndex < 0 || fileIndex >= files.Count) return;

        var file = files[fileIndex];

        for (int s = 0; s < file.streamlines.Count; s++)
        {
            var streamline = file.streamlines[s];

            for (int i = 0; i < particlesPerStreamline; i++)
            {
                float startDist = Random.Range(0f, streamline.totalLength);
                Vector3 startPos = Evaluate(streamline, startDist) + sceneOffset;

                var obj = Instantiate(particlePrefab, startPos, Quaternion.identity, particleParent);

                var trail = obj.GetComponent<TrailRenderer>();
                if (trail == null)
                    trail = obj.AddComponent<TrailRenderer>();

                trail.time           = trailDuration;
                trail.startWidth     = trailWidth;
                trail.endWidth       = 0f;
                trail.numCapVertices = 4;
                trail.material       = _trailMat;

                particles.Add(new ParticleState
                {
                    transform       = obj.transform,
                    trail           = trail,
                    fileIndex       = fileIndex,
                    streamlineIndex = s,
                    distanceAlong   = startDist,
                    looping         = true
                });
            }
        }
    }

    void UpdateParticles(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];

            if (p.fileIndex >= files.Count) continue;

            var file = files[p.fileIndex];

            if (p.streamlineIndex >= file.streamlines.Count) continue;

            var s = file.streamlines[p.streamlineIndex];

            float d = p.distanceAlong;

            p.transform.position = Evaluate(s, d) + sceneOffset;

            // color trail: 1=upstream freestream (red), 0=wake (blue)
            float colorT   = s.totalLength > 0f ? Mathf.Clamp01(d / s.totalLength) : 0f;
            Color windColor = windGradient.Evaluate(colorT);
            if (p.trail != null)
            {
                p.trail.startColor = windColor;
                p.trail.endColor   = new Color(windColor.r, windColor.g, windColor.b, 0f);
            }

            d -= speed * dt;

            float disableThreshold = trailDisableTime * speed;

            if (!p.trailDisabled && d <= disableThreshold)
            {
                if (p.trail != null)
                    p.trail.enabled = false;

                p.trailDisabled = true;
            }

            if (d <= 0f)
            {
                if (p.looping)
                {
                    d = s.totalLength;

                    if (p.trail != null)
                        StartCoroutine(ReenableTrail(p));

                    p.trailDisabled = false;
                }
                else
                {
                    Destroy(p.transform.gameObject);
                    particles.RemoveAt(i);
                    continue;
                }
            }

            p.distanceAlong = d;
        }
    }

    IEnumerator ReenableTrail(ParticleState p)
    {
        yield return null;

        if (p.trail != null)
        {
            p.trail.Clear();
            p.trail.enabled = true;
        }
    }

    Vector3 Evaluate(Streamline s, float distance)
    {
        if (distance <= 0f) return s.points[0];
        if (distance >= s.totalLength) return s.points[^1];

        int index = FindSegmentIndex(s.distances, distance);

        float d0 = s.distances[index];
        float d1 = s.distances[index + 1];

        float t = Mathf.InverseLerp(d0, d1, distance);

        int count = s.points.Count;

        int p0 = Mathf.Clamp(index - 1, 0, count - 1);
        int p1 = Mathf.Clamp(index, 0, count - 1);
        int p2 = Mathf.Clamp(index + 1, 0, count - 1);
        int p3 = Mathf.Clamp(index + 2, 0, count - 1);

        return CatmullRom(s.points[p0], s.points[p1], s.points[p2], s.points[p3], t);
    }

    int FindSegmentIndex(List<float> distances, float d)
    {
        int low  = 0;
        int high = distances.Count - 2;

        while (low <= high)
        {
            int mid = (low + high) >> 1;

            if (d < distances[mid])
                high = mid - 1;
            else if (d > distances[mid + 1])
                low = mid + 1;
            else
                return mid;
        }

        return Mathf.Clamp(low, 0, distances.Count - 2);
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }
}
