// SimDataLoader.cs
// =================
// Loads a pre-baked binary .simdata file (produced by csv_to_simdata.py)
// and feeds the parsed streamline data directly into StreamlineSystem,
// replacing its CSV TextAsset loading with fast binary reads.
//
// Why binary over CSV?
//   A CSV with 100k points takes ~50ms to parse in C# (string splits, float.Parse).
//   The equivalent binary file loads in ~2ms — no string parsing, direct memory reads.
//
// Setup:
//   1. Run csv_to_simdata.py to convert your wind/wave CSV to a .simdata file.
//   2. Place the .simdata file in Assets/StreamingAssets/  (Unity serves these at runtime).
//   3. Attach SimDataLoader to the same GameObject as StreamlineSystem.
//   4. Set fileName to your .simdata filename (e.g. "wind.simdata").
//   5. The loader will override StreamlineSystem's CSV loading automatically on Start().
//
// The .simdata binary format (little-endian):
//   [4B] magic "SIMD"
//   [4B] version int32
//   [4B] streamline count int32
//   per streamline:
//     [4B] point count int32
//     per point: x, y, z, vx, vy, vz — six float32s (24 bytes)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(StreamlineSystem))]
public class SimDataLoader : MonoBehaviour
{
    [Header("Binary Data File")]
    [Tooltip("Filename inside Assets/StreamingAssets/ — e.g. 'wind.simdata'")]
    public string fileName = "wind.simdata";

    [Header("Coordinate Settings")]
    [Tooltip("Applies the same -90 X rotation as StreamlineSystem to convert OpenFOAM coords.")]
    public bool applyCoordinateRotation = true;

    // Parsed data — exposed so StreamlineSystem (or anything else) can read it
    [HideInInspector] public List<List<Vector3>> LoadedPaths      = new List<List<Vector3>>();
    [HideInInspector] public List<List<Vector3>> LoadedVelocities = new List<List<Vector3>>();
    [HideInInspector] public bool IsLoaded { get; private set; } = false;

    private static readonly byte[] MAGIC_BYTES = { (byte)'S', (byte)'I', (byte)'M', (byte)'D' };

    StreamlineSystem streamline;

    // -------------------------------------------------------------------------

    void Awake()
    {
        streamline = GetComponent<StreamlineSystem>();
    }

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_ANDROID || UNITY_WEBGL
        // On Android/WebGL, StreamingAssets must be read via UnityWebRequest
        StartCoroutine(LoadAsync(path));
#else
        LoadSync(path);
        InjectIntoStreamlineSystem();
#endif
    }

    // -------------------------------------------------------------------------
    // Sync load (Editor, Windows, Mac, Linux, iOS)
    // -------------------------------------------------------------------------

    void LoadSync(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"[SimDataLoader] File not found: {path}\n" +
                           $"Place your .simdata file in Assets/StreamingAssets/");
            return;
        }

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            ParseBinary(reader);
        }

        Debug.Log($"[SimDataLoader] Loaded '{fileName}' — " +
                  $"{LoadedPaths.Count} streamlines from binary.");
        IsLoaded = true;
    }

    // -------------------------------------------------------------------------
    // Async load (Android / WebGL)
    // -------------------------------------------------------------------------

    IEnumerator LoadAsync(string path)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SimDataLoader] Failed to load '{fileName}': {req.error}");
                yield break;
            }

            using (BinaryReader reader = new BinaryReader(
                       new MemoryStream(req.downloadHandler.data)))
            {
                ParseBinary(reader);
            }

            Debug.Log($"[SimDataLoader] Async loaded '{fileName}' — " +
                      $"{LoadedPaths.Count} streamlines.");
            IsLoaded = true;
            InjectIntoStreamlineSystem();
        }
    }

    // -------------------------------------------------------------------------
    // Binary parser
    // -------------------------------------------------------------------------

    void ParseBinary(BinaryReader reader)
    {
        // Validate magic number
        byte[] magic = reader.ReadBytes(4);
        for (int i = 0; i < 4; i++)
        {
            if (magic[i] != MAGIC_BYTES[i])
            {
                Debug.LogError("[SimDataLoader] Invalid .simdata file — magic number mismatch. " +
                               "Re-run csv_to_simdata.py to regenerate.");
                return;
            }
        }

        int version = reader.ReadInt32();
        if (version != 1)
            Debug.LogWarning($"[SimDataLoader] Unexpected version {version}, expected 1. Proceeding anyway.");

        int streamlineCount = reader.ReadInt32();
        Quaternion rotation = applyCoordinateRotation
            ? Quaternion.Euler(-90f, 0f, 0f)
            : Quaternion.identity;

        LoadedPaths.Clear();
        LoadedVelocities.Clear();

        for (int s = 0; s < streamlineCount; s++)
        {
            int pointCount = reader.ReadInt32();

            var path = new List<Vector3>(pointCount);
            var vels = new List<Vector3>(pointCount);

            for (int p = 0; p < pointCount; p++)
            {
                float x  = reader.ReadSingle();
                float y  = reader.ReadSingle();
                float z  = reader.ReadSingle();
                float vx = reader.ReadSingle();
                float vy = reader.ReadSingle();
                float vz = reader.ReadSingle();

                path.Add(rotation * new Vector3(x, y, z));
                vels.Add(rotation * new Vector3(vx, vy, vz));
            }

            LoadedPaths.Add(path);
            LoadedVelocities.Add(vels);
        }
    }

    // -------------------------------------------------------------------------
    // Inject parsed data into StreamlineSystem
    // -------------------------------------------------------------------------

    void InjectIntoStreamlineSystem()
    {
        if (streamline == null || !IsLoaded) return;

        // Mirror the combined-path structure StreamlineSystem builds from CSV:
        // forward half + backward half merged into one continuous path per streamline.
        // Since csv_to_simdata already preserves per-ID ordering, we inject directly.
        streamline.InjectPaths(LoadedPaths, LoadedVelocities);
    }
}
