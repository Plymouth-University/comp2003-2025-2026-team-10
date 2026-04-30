using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class WaveDataLoader : MonoBehaviour
{
    [HideInInspector] public Texture2D[] heightmaps;
    [HideInInspector] public Texture2D[] flowMaps;
    [HideInInspector] public int frameCount;
    [HideInInspector] public bool isLoaded = false;

    void Start()
    {
        StartCoroutine(LoadAll());
    }

    IEnumerator LoadAll()
    {
        // Read metadata
        string metaPath = Path.Combine(Application.streamingAssetsPath, "flowmaps/metadata.json");
        string metaJson = File.ReadAllText(metaPath);
        MetaData meta = JsonUtility.FromJson<MetaData>(metaJson);
        frameCount = meta.frame_count;

        Debug.Log($"Loading {frameCount} frames...");

        heightmaps = new Texture2D[frameCount];
        flowMaps   = new Texture2D[frameCount - 1];

        // Load heightmaps
        for (int i = 1; i <= frameCount; i++)
        {
            string path = Path.Combine(Application.streamingAssetsPath, $"heightmaps/heightmap_{i}.png");
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.R8, false);
            tex.LoadImage(bytes);
            heightmaps[i - 1] = tex;
            yield return null;
        }

        // Load flow maps
        for (int i = 0; i < frameCount - 1; i++)
        {
            string path = Path.Combine(Application.streamingAssetsPath, $"flowmaps/flow_{i:D4}.exr");
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBAFloat, false);
            tex.LoadImage(bytes);
            flowMaps[i] = tex;
            yield return null;
        }

        isLoaded = true;
        Debug.Log("All frames loaded successfully.");
    }

    [System.Serializable]
    private class MetaData
    {
        public int frame_count;
        public int flow_count;
    }
}
