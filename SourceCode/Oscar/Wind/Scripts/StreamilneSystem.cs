using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class StreamlineSystem : MonoBehaviour
{
    public TextAsset csvFile;

    public GameObject particlePrefab;

    public Transform particleParent;

    public float speed = 100f;

    public int particlesPerStreamline = 1;

    Dictionary<int, List<Vector3>> paths = new Dictionary<int, List<Vector3>>();
    Dictionary<int, List<Vector3>> velocities = new Dictionary<int, List<Vector3>>();

    List<List<Vector3>> combinedPaths = new List<List<Vector3>>();
    List<List<Vector3>> combinedVelocities = new List<List<Vector3>>();

    List<Transform> particles = new List<Transform>();
    List<float> particleDistances = new List<float>();
    List<TrailRenderer> trails = new List<TrailRenderer>();

    void Start()
    {
        LoadCSV();
        BuildCombinedPaths();
        SpawnParticles();
    }

    void Update()
    {
        float dt = Time.deltaTime * speed;
        
        UpdateParticles(dt);
    }

    void LoadCSV()
    {
        string[] lines = csvFile.text.Split('\n');
        Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] p = lines[i].Split(',');

            float time = float.Parse(p[0], CultureInfo.InvariantCulture);
            int id = int.Parse(p[4]);

            float vx = float.Parse(p[1], CultureInfo.InvariantCulture);
            float vy = float.Parse(p[2], CultureInfo.InvariantCulture);
            float vz = float.Parse(p[3], CultureInfo.InvariantCulture);

            float x = float.Parse(p[5], CultureInfo.InvariantCulture);
            float y = float.Parse(p[6], CultureInfo.InvariantCulture);
            float z = float.Parse(p[7], CultureInfo.InvariantCulture);

            Vector3 pos = rotation * new Vector3(x, y, z);
            Vector3 vel = rotation * new Vector3(vx, vy, vz);

            if (!paths.ContainsKey(id))
            {
                paths[id] = new List<Vector3>();
                velocities[id] = new List<Vector3>();
            }

            paths[id].Add(pos);
            velocities[id].Add(vel);
        }

        Debug.Log("Loaded streamlines: " + paths.Count);
    }

    // Combine both the 'BACKWARDS' and 'FORWARDS' streamline to create one continuous streamline
    void BuildCombinedPaths()
    {
        int half = paths.Count / 2;

        for (int i = 0; i < half; i++)
        {
            if (!paths.ContainsKey(i) || !paths.ContainsKey(i + half))
                continue;

            List<Vector3> backward = paths[i];
            List<Vector3> forward = paths[i + half];

            List<Vector3> backwardVel = velocities[i];
            List<Vector3> forwardVel = velocities[i + half];

            List<Vector3> combined = new List<Vector3>();
            List<Vector3> combinedVel = new List<Vector3>();

            for (int j = backward.Count - 1; j >= 0; j--)
            {
                combined.Add(backward[j]);
                combinedVel.Add(backwardVel[j]);
            }

            for (int j = 0; j < forward.Count; j++)
            {
                combined.Add(forward[j]);
                combinedVel.Add(forwardVel[j]);
            }

            combinedPaths.Add(combined);
            combinedVelocities.Add(combinedVel);
        }

        Debug.Log("Combined streamline paths: " + combinedPaths.Count);
    }

    void SpawnParticles()
    {
        foreach (var path in combinedPaths)
        {
            for (int i = 0; i < particlesPerStreamline; i++)
            {
                GameObject p = Instantiate(
                    particlePrefab,
                    path[0],
                    Quaternion.identity,
                    particleParent
                );

                particles.Add(p.transform);

                trails.Add(p.GetComponent<TrailRenderer>());

                //Spawn particle in random position along the streamline
                particleDistances.Add(
                    Random.Range(0f, path.Count - 1f)
                );
            }
        }
    }

    void UpdateParticles(float dt)
    {
        int particleIndex = 0;

        int streamlineCount = combinedPaths.Count;

        for (int s = 0; s < streamlineCount; s++)
        {
            var path = combinedPaths[s];
            var vel = combinedVelocities[s];

            int pointCount = path.Count;

            for (int i = 0; i < particlesPerStreamline; i++)
            {
                Transform particle = particles[particleIndex];

                float d = particleDistances[particleIndex];

                int a = Mathf.FloorToInt(d);
                int b = Mathf.Min(a + 1, pointCount - 1);

                float t = d - a;

                // Catmull-Rom interpolation for smoother movement
                int p0 = Mathf.Max(a - 1, 0);
                int p1 = a;
                int p2 = b;
                int p3 = Mathf.Min(b + 1, pointCount - 1);

                Vector3 pos = CatmullRom(
                    path[p0],
                    path[p1],
                    path[p2],
                    path[p3],
                    t
                );

                // Linear interpolation for velocity
                Vector3 velocity = Vector3.Lerp(vel[a], vel[b], t);

                particle.position = pos;

                float step = velocity.magnitude * dt;

                d -= step;
                d = Mathf.Repeat(d, pointCount - 1);

                particleDistances[particleIndex] = d;

                particleIndex++;
            }
        }
    }

    // Catmull-Rom spline interpolation
    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f*p0 - 5f*p1 + 4f*p2 - p3) * t * t +
            (-p0 + 3f*p1 - 3f*p2 + p3) * t * t * t
        );
    }    
}
