using UnityEngine;

public class WaterAnimator : MonoBehaviour
{
    [Header("Assets")]
    public GameObject[] frames;
    public float framesPerSecond = 10f;
    public bool interpolate = true;

    [Header("Visual Tweaks")]
    public float waveHeightScale = 1.0f;
    public bool flipVertical = false;

    [Header("Turbine (FOWT) Position")]
    public Transform turbineTransform;
    public bool useFixedHorizontalPos = true;
    public float targetX = 10f;
    public float targetZ = -0.2624838f;
    public float turbineHeightOffset = 0f;

    [Header("Sway/Tilt Settings")]
    public bool enableSway = true;
    public float pitchIntensity = 5.0f;
    public float rollIntensity = 2.0f;
    public float swaySmoothness = 2.0f;

    private MeshFilter meshFilter;
    private Mesh dynamicMesh;
    private Mesh[] meshCache;
    private Vector3[] lerpedVertices;
    private float lastHeight = 0f;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (frames == null || frames.Length == 0) return;

        // Correct orientation for ParaView Z -> Unity Y
        transform.localEulerAngles = new Vector3(flipVertical ? 90 : -90, 0, 0);

        meshCache = new Mesh[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                meshCache[i] = frames[i].GetComponentInChildren<MeshFilter>().sharedMesh;
        }

        // Initialize empty mesh
        dynamicMesh = new Mesh();
        dynamicMesh.MarkDynamic();
        meshFilter.mesh = dynamicMesh;
    }

    void Update()
    {
        if (meshCache == null || meshCache.Length < 2) return;

        float time = Time.time * framesPerSecond;
        int indexA = Mathf.FloorToInt(time) % meshCache.Length;
        int indexB = (indexA + 1) % meshCache.Length;
        float fraction = time - Mathf.Floor(time);

        Mesh m1 = meshCache[indexA];
        Mesh m2 = meshCache[indexB];

        // 1. CHECK FOR CHANGES
        // If the vertex count changes between frames, we MUST rebuild triangles.
        // If we don't, Unity throws the "vertices too small" error.
        if (interpolate && m1.vertexCount == m2.vertexCount)
        {
            if (lerpedVertices == null || lerpedVertices.Length != m1.vertexCount)
            {
                RebuildMeshStructure(m1);
            }

            Vector3[] v1 = m1.vertices;
            Vector3[] v2 = m2.vertices;

            for (int i = 0; i < v1.Length; i++)
            {
                Vector3 lerped = Vector3.Lerp(v1[i], v2[i], fraction);
                lerped.z *= waveHeightScale;
                lerpedVertices[i] = lerped;
            }
            dynamicMesh.vertices = lerpedVertices;
        }
        else
        {
            // If counts are different, snap to frame A and rebuild
            if (lerpedVertices == null || lerpedVertices.Length != m1.vertexCount)
            {
                RebuildMeshStructure(m1);
            }
            else
            {
                Vector3[] v1 = m1.vertices;
                for(int i = 0; i < v1.Length; i++) v1[i].z *= waveHeightScale;
                dynamicMesh.vertices = v1;
                lerpedVertices = v1;
            }
        }

        dynamicMesh.RecalculateBounds();
        dynamicMesh.RecalculateNormals();

        // --- TURBINE LOGIC ---
        if (turbineTransform != null && lerpedVertices != null && lerpedVertices.Length > 0)
        {
            // Sample height from the middle of the current frame
            int middleIndex = lerpedVertices.Length / 2;
            float localWaveZ = lerpedVertices[middleIndex].z;

            float verticalMovement = localWaveZ * (flipVertical ? 1 : -1);
            float currentHeight = transform.position.y + verticalMovement + turbineHeightOffset;

            float velocity = (Time.deltaTime > 0) ? (currentHeight - lastHeight) / Time.deltaTime : 0;
            lastHeight = currentHeight;

            if (useFixedHorizontalPos)
            {
                turbineTransform.position = new Vector3(targetX, currentHeight, targetZ);
            }

            if (enableSway)
            {
                float targetPitch = velocity * pitchIntensity;
                float targetRoll = Mathf.Sin(Time.time) * rollIntensity;
                Quaternion targetRot = Quaternion.Euler(targetPitch, turbineTransform.eulerAngles.y, targetRoll);
                turbineTransform.rotation = Quaternion.Slerp(turbineTransform.rotation, targetRot, Time.deltaTime * swaySmoothness);
            }
        }
    }

    void RebuildMeshStructure(Mesh source)
    {
        // THIS IS THE FIX: Clear everything so the "too small" check is bypassed.
        dynamicMesh.Clear();

        Vector3[] v = source.vertices;
        for(int i = 0; i < v.Length; i++) v[i].z *= waveHeightScale;

        dynamicMesh.vertices = v;
        dynamicMesh.triangles = source.triangles;
        dynamicMesh.uv = source.uv;
        lerpedVertices = v;
    }
}
