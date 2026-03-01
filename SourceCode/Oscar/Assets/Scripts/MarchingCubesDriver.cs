using UnityEngine;

public class MarchingCubesDriver : MonoBehaviour
{
    public ComputeShader marchingShader;
    public RenderTexture sdf;

    public int resolution = 128;
    public float isoLevel = 0f;

    ComputeBuffer vertexBuffer;
    ComputeBuffer normalBuffer;

    const int MAX_VERTS = 5_000_000;

    void Start()
    {
        vertexBuffer = new ComputeBuffer(MAX_VERTS, sizeof(float)*3,
            ComputeBufferType.Append);
        normalBuffer = new ComputeBuffer(MAX_VERTS, sizeof(float)*3,
            ComputeBufferType.Append);
    }

    public void Run()
    {
        vertexBuffer.SetCounterValue(0);
        normalBuffer.SetCounterValue(0);

        int k = marchingShader.FindKernel("March");

        marchingShader.SetTexture(k, "_SDF", sdf);
        marchingShader.SetBuffer(k, "_Vertices", vertexBuffer);
        marchingShader.SetBuffer(k, "_Normals", normalBuffer);

        marchingShader.SetInt("_Resolution", resolution);
        marchingShader.SetFloat("_IsoLevel", isoLevel);

        marchingShader.Dispatch(k, resolution/8, resolution/8, resolution/8);
    }
}