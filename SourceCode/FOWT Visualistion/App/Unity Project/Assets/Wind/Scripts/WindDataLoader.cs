using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class WindDataLoader : MonoBehaviour
{
    // CSV column layout (11 columns):
    // 0: alpha.water  1-3: UnityVector:0/1/2  4-6: U:0/1/2  7: UMag  8-10: Points:0/1/2

    [Header("Data Source")]
    public List<TextAsset> csvFrames;
    public float baseFrameRate = 1f;      // frames/sec at 1 m/s average wind; scales with avg speed

    [Header("Visual Tuning")]
    public int maxArrows = 200;
    public GameObject arrowPrefab;
    public float arrowLength = 1f;        // fixed length for all arrows regardless of speed
    public Vector3 thicknessScale = new Vector3(0.12f, 0.12f, 1f);
    public Vector3 sceneOffset = Vector3.zero;

    [Header("Coloring")]
    public Gradient windGradient;
    public float maxWindSpeedForGradient = 2.0f;

    private struct WindPoint
    {
        public Vector3 position;
        public Vector3 direction;
        public float speed;
    }

    private class FrameData
    {
        public WindPoint[] points;
        public float avgSpeed;
    }

    private List<FrameData> _frames = new List<FrameData>();
    private Transform[] _pool;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private float _timeAccum = 0f;

    void Start()
    {
        SetupGradient();
        _propBlock = new MaterialPropertyBlock();
        PreloadData();
        InitializePool();
    }

    void SetupGradient()
    {
        // Always initialise blue (slow) → red (fast) to match ParaView convention
        GradientColorKey[] colorKeys =
        {
            new GradientColorKey(new Color(0.05f, 0.3f, 1f),  0f),
            new GradientColorKey(new Color(1f,    0.1f, 0.05f), 1f)
        };
        GradientAlphaKey[] alphaKeys =
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };
        windGradient.SetKeys(colorKeys, alphaKeys);
    }

    void PreloadData()
    {
        foreach (var csv in csvFrames)
        {
            if (csv == null) continue;

            var points = new List<WindPoint>();
            string[] lines = csv.text.Split('\n');
            float speedSum = 0f;

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cols = lines[i].Split(',');
                if (cols.Length < 11) continue;

                try
                {
                    float alphaWater = float.Parse(cols[0], CultureInfo.InvariantCulture);
                    if (alphaWater > 0.5f) continue;

                    float speed = float.Parse(cols[7], CultureInfo.InvariantCulture);
                    if (speed < 0.01f) continue;

                    // OpenFOAM → Unity: X stays, Z→Y, Y→-Z
                    float px = float.Parse(cols[8],  CultureInfo.InvariantCulture);
                    float py = float.Parse(cols[9],  CultureInfo.InvariantCulture);
                    float pz = float.Parse(cols[10], CultureInfo.InvariantCulture);

                    // UnityVector (cols 1-3) already in Unity space from ParaView
                    float dx = float.Parse(cols[1], CultureInfo.InvariantCulture);
                    float dy = float.Parse(cols[2], CultureInfo.InvariantCulture);
                    float dz = float.Parse(cols[3], CultureInfo.InvariantCulture);

                    points.Add(new WindPoint
                    {
                        position  = new Vector3(px, pz, -py),
                        direction = new Vector3(dx, dy, dz),
                        speed     = speed
                    });
                    speedSum += speed;
                }
                catch { continue; }
            }

            float avg = points.Count > 0 ? speedSum / points.Count : 1f;
            _frames.Add(new FrameData { points = points.ToArray(), avgSpeed = avg });
        }
    }

    void InitializePool()
    {
        _pool      = new Transform[maxArrows];
        _renderers = new Renderer[maxArrows];

        for (int i = 0; i < maxArrows; i++)
        {
            GameObject go = Instantiate(arrowPrefab, transform);
            _pool[i]      = go.transform;
            _renderers[i] = go.GetComponentInChildren<Renderer>();
            go.SetActive(false);
        }
    }

    void Update()
    {
        if (_frames.Count == 0 || _pool == null) return;

        int frameIdx = (int)_timeAccum % _frames.Count;
        FrameData frame = _frames[frameIdx];

        // advance time proportional to this frame's average wind speed —
        // higher wind = faster cycling so you feel the energy change
        _timeAccum += Time.deltaTime * baseFrameRate * frame.avgSpeed;

        WindPoint[] data = frame.points;

        for (int i = 0; i < _pool.Length; i++)
        {
            if (i < data.Length)
            {
                if (!_pool[i].gameObject.activeSelf)
                    _pool[i].gameObject.SetActive(true);

                _pool[i].position = data[i].position + sceneOffset;

                if (data[i].direction.sqrMagnitude > 0.001f)
                    _pool[i].rotation = Quaternion.LookRotation(data[i].direction);

                // uniform length — speed communicated through colour only
                _pool[i].localScale = new Vector3(thicknessScale.x, thicknessScale.y, arrowLength);

                if (_renderers[i] != null)
                {
                    float t = Mathf.Clamp01(data[i].speed / maxWindSpeedForGradient);
                    _propBlock.SetColor("_BaseColor", windGradient.Evaluate(t));
                    _renderers[i].SetPropertyBlock(_propBlock);
                }
            }
            else
            {
                if (_pool[i].gameObject.activeSelf)
                    _pool[i].gameObject.SetActive(false);
            }
        }
    }
}
