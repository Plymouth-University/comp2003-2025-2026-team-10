using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class ForwardStreamlineSystem : MonoBehaviour
{
    [Header("Rendering")]
    public GameObject particlePrefab;
    public Transform particleParent;

    [Header("Simulation")]
    public float speed = 3f;
    public int particlesPerStreamline = 1;

    [Header("File Control")]
    public float fileSwitchInterval = 5f;

    float trailDisableTime = 0.5f;

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

        public int streamlineIndex;
        public int fileIndex;

        public float distance;
        public bool looping;
        public bool trailDisabled;
    }

    readonly List<StreamlineFile> files = new();
    readonly List<ParticleState> particles = new();

    int currentFile = 0;
    float fileTimer = 0f;

    void Start()
    {
        LoadFiles();
        if (files.Count > 0)
            StartFile(0);
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

            Debug.Log($"Switching to file index: {currentFile}");

            StartFile(currentFile);
        }
    }

    //Load CSV files
    void LoadFiles()
    {
        TextAsset[] csvFiles = Resources.LoadAll<TextAsset>("Streamline");
        Quaternion rot = Quaternion.Euler(-90f, 0f, 0f);

        foreach (var csv in csvFiles)
        {
            Dictionary<int, List<Vector3>> rawPaths = new();

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
                if (!rawPaths.TryGetValue(i, out var path)) continue;

                if (path.Count < 2) continue;

                file.streamlines.Add(BuildArcLengthStreamline(path));
            }

            files.Add(file);
        }

        Debug.Log($"Loaded {files.Count} files");
    }

    //Build streanline
    Streamline BuildArcLengthStreamline(List<Vector3> pts)
    {
        var s = new Streamline
        {
            points = pts,
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

    //Start file
    void StartFile(int fileIndex)
    {
        if (fileIndex < 0 || fileIndex >= files.Count) return;

        var file = files[fileIndex];

        Debug.Log($"Starting file index: {fileIndex}");

        for (int s = 0; s < file.streamlines.Count; s++)
        {
            var streamline = file.streamlines[s];

            if (streamline.points.Count < 4) continue;

            for (int i = 0; i < particlesPerStreamline; i++)
            {
                float startDist = Random.Range(0f, streamline.totalLength);
                Vector3 startPos = Evaluate(streamline, startDist);

                var obj = Instantiate(particlePrefab, startPos, Quaternion.identity, particleParent);

                particles.Add(new ParticleState
                {
                    transform = obj.transform,
                    trail = obj.GetComponent<TrailRenderer>(),
                    streamlineIndex = s,
                    fileIndex = fileIndex,
                    distance = startDist,
                    looping = true
                });
            }
        }
    }

    //Update particles
    void UpdateParticles(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];

            if (p.fileIndex >= files.Count) continue;

            var file = files[p.fileIndex];

            if (p.streamlineIndex >= file.streamlines.Count) continue;

            var s = file.streamlines[p.streamlineIndex];

            float d = p.distance;

            Vector3 pos = Evaluate(s, d);
            p.transform.position = pos;

            // Arc-length movement
            d += speed * dt;

            float endThreshold = s.totalLength;
            float disableThreshold = endThreshold - trailDisableTime * speed;

            if (!p.trailDisabled && d >= disableThreshold)
            {
                if (p.trail != null)
                    p.trail.enabled = false;

                p.trailDisabled = true;
            }

            if (d >= endThreshold)
            {
                if (p.looping)
                {
                    d = 0f;

                    StartCoroutine(ReenableTrailNextFrame(p));
                    p.trailDisabled = false;
                }
                else
                {
                    Destroy(p.transform.gameObject);
                    particles.RemoveAt(i);
                    continue;
                }
            }

            p.distance = d;
        }
    }

    IEnumerator ReenableTrailNextFrame(ParticleState p)
    {
        yield return null;

        if (p.trail != null)
            p.trail.enabled = true;
    }

    //Arc length
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
        int low = 0;
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

    //Catmull-Rom interpolation
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