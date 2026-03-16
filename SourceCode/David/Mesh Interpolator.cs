using UnityEngine;
using System.Collections.Generic;

public class MeshInterpolator : MonoBehaviour
{
    public Mesh[] snapshots; // Drag all your exported OBJs here in order
    [Range(0, 10)] public float playbackSpeed = 1f;

    private MeshFilter meshFilter;
    private Mesh interpolatedMesh;
    private Vector3[] lerpedVertices;
    private float timer;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        // Create a unique mesh instance so we don't overwrite the original assets
        interpolatedMesh = Instantiate(snapshots[0]);
        meshFilter.mesh = interpolatedMesh;
        lerpedVertices = new Vector3[snapshots[0].vertexCount];
    }

    void Update()
    {
        if (snapshots.Length < 2) return;

        // 1. Calculate timing
        timer = (timer + Time.deltaTime * playbackSpeed) % (snapshots.Length - 1);
        int idx = Mathf.FloorToInt(timer);
        float fraction = timer % 1.0f;

        // 2. Interpolate every vertex position
        Vector3[] vA = snapshots[idx].vertices;
        Vector3[] vB = snapshots[idx + 1].vertices;

        for (int i = 0; i < lerpedVertices.Length; i++)
        {
            lerpedVertices[i] = Vector3.Lerp(vA[i], vB[i], fraction);
        }

        // 3. Update the mesh
        interpolatedMesh.vertices = lerpedVertices;
        
        // Only recalculate bounds if the movement is large
        interpolatedMesh.RecalculateBounds();
    }
}
