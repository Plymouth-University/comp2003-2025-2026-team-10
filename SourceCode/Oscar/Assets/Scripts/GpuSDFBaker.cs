using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

public class GpuSDFBakerTool : EditorWindow
{
    ComputeShader sdfShader;
    Mesh[] meshes;

    int resolution = 128;
    int distancePasses = 20;
    string saveFolder = "Assets/SDFBakes";

    RenderTexture volume;

    [MenuItem("Tools/Fluid/GPU SDF Baker")]
    static void Open()
    {
        GetWindow<GpuSDFBakerTool>("GPU SDF Baker");
    }

    void OnGUI()
    {
        GUILayout.Label("GPU SDF Baker", EditorStyles.boldLabel);

        sdfShader = (ComputeShader)EditorGUILayout.ObjectField(
            "SDF Compute", sdfShader, typeof(ComputeShader), false);

        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 32, 256);
        distancePasses = EditorGUILayout.IntSlider("Distance Passes", distancePasses, 4, 64);

        GUILayout.Space(5);

        if (GUILayout.Button("Use Selected Meshes"))
        {
            var filters = Selection.GetFiltered<MeshFilter>(SelectionMode.Deep);

            var list = new System.Collections.Generic.List<Mesh>();

            foreach (var f in filters)
            {
                if (f.sharedMesh != null)
                    list.Add(f.sharedMesh);
            }

            meshes = list.ToArray();

            Debug.Log($"GPU SDF Baker: Loaded {meshes.Length} meshes.");
        }

        if (meshes != null)
            EditorGUILayout.LabelField("Meshes Loaded", meshes.Length.ToString());

        saveFolder = EditorGUILayout.TextField("Save Folder", saveFolder);

        GUILayout.Space(10);

        GUI.enabled = sdfShader != null && meshes != null && meshes.Length > 0;

        if (GUILayout.Button("Bake All"))
        {
            BakeAll();
        }

        GUI.enabled = true;
    }


    void InitVolume()
    {
        if (volume != null && volume.width == resolution) return;

        if (volume != null) volume.Release();

        volume = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
        volume.dimension = TextureDimension.Tex3D;
        volume.volumeDepth = resolution;
        volume.enableRandomWrite = true;
        volume.wrapMode = TextureWrapMode.Clamp;
        volume.Create();
    }


    void BakeAll()
    {
        if (!Directory.Exists(saveFolder))
            Directory.CreateDirectory(saveFolder);

        InitVolume();

        try
        {
            for (int m = 0; m < meshes.Length; m++)
            {
                EditorUtility.DisplayProgressBar(
                    "GPU SDF Bake",
                    meshes[m].name,
                    m / (float)meshes.Length);

                BakeMesh(meshes[m], m);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log("GPU SDF bake complete.");
        }
    }


    void BakeMesh(Mesh mesh, int index)
    {
        //buffers
        ComputeBuffer vBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3);
        ComputeBuffer tBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));

        vBuffer.SetData(mesh.vertices);
        tBuffer.SetData(mesh.triangles);

        Bounds bounds = mesh.bounds;

        //clear
        int kClear = sdfShader.FindKernel("Clear");
        sdfShader.SetTexture(kClear, "_Volume", volume);
        sdfShader.SetInt("_Resolution", resolution);
        sdfShader.SetFloat("_MaxDistance", 999f);
        sdfShader.Dispatch(kClear, resolution / 8, resolution / 8, resolution / 8);

        //voxelize
        int kVox = sdfShader.FindKernel("Voxelize");
        sdfShader.SetBuffer(kVox, "_Vertices", vBuffer);
        sdfShader.SetBuffer(kVox, "_Triangles", tBuffer);
        sdfShader.SetVector("_BoundsMin", bounds.min);
        sdfShader.SetVector("_BoundsSize", bounds.size);
        sdfShader.Dispatch(kVox, mesh.triangles.Length / 3 / 64 + 1, 1, 1);

        //distance passes
        int kDist = sdfShader.FindKernel("DistancePass");
        for (int i = 0; i < distancePasses; i++)
        {
            sdfShader.SetTexture(kDist, "_Volume", volume);
            sdfShader.Dispatch(kDist, resolution / 8, resolution / 8, resolution / 8);
        }

        SaveTexture($"{mesh.name}_frame{index:D2}");

        vBuffer.Release();
        tBuffer.Release();
    }


    void SaveTexture(string meshName)
    {
        Texture3D tex = new Texture3D(resolution, resolution, resolution, TextureFormat.RFloat, false);

        //readback
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = volume;

        Texture2D temp = new Texture2D(resolution, resolution, TextureFormat.RFloat, false);

        Color[] colors = new Color[resolution * resolution * resolution];

        temp.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        temp.Apply();

        for (int i = 0; i < colors.Length; i++)
            colors[i] = new Color(0, 0, 0, 0);

        tex.SetPixels(colors);
        tex.Apply();

        RenderTexture.active = prev;

        string path = $"{saveFolder}/{meshName}_SDF.asset";
        AssetDatabase.CreateAsset(tex, path);
    }
}