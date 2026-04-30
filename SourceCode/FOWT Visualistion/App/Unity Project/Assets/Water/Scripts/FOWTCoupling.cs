using UnityEngine;

public class FOWTCoupling : MonoBehaviour
{
    [Header("References")]
    public WaterSurface waterSurface;
    public WaveDataLoader loader;

    [Header("Platform UV Position")]
    public Vector2 platformUV = new Vector2(0.5f, 0.5f);

    [Header("Motion Settings")]
    public float heaveScale  = 0.5f;
    public float pitchScale  = 3f;
    public float rollScale   = 3f;
    public float smoothing   = 4f;

    private Vector3 basePosition = new Vector3(0.8f, 0f, 0f);
    private float smoothedHeight;

    void Start()
    {
        smoothedHeight = 0f;
    }

    void Update()
    {
        if (!loader.isLoaded) return;

        float sampleOffset = 0.02f;

        float h      = waterSurface.GetHeightAtUV(platformUV);
        float hRight = waterSurface.GetHeightAtUV(platformUV + new Vector2(sampleOffset, 0));
        float hUp    = waterSurface.GetHeightAtUV(platformUV + new Vector2(0, sampleOffset));

        float slopeX = (hRight - h) / sampleOffset;
        float slopeZ = (hUp    - h) / sampleOffset;

        smoothedHeight = Mathf.Lerp(smoothedHeight, h, Time.deltaTime * smoothing);

        Vector3 pos = basePosition;
        pos.y = (smoothedHeight - 0.5f) * heaveScale;
        transform.position = pos;

        transform.rotation = Quaternion.Euler(
            -slopeZ * pitchScale,
            transform.eulerAngles.y,
            -slopeX * rollScale
        );
    }
}
