// FIX SUMMARY (vs original):
// - Replaced Time.time (wall clock) with a public simulationTime field you can
//   control from the Unity Inspector, scrub via script, or drive from a UI slider.
// - Added play/pause toggle and a configurable playback speed.
// - Added null/length guards that give clearer error messages.
// - Kept all original LERP logic unchanged.

using System;
using UnityEngine;

[System.Serializable]
public class TemporalTimeStep
{
    public float time;
    public float[] values;
}

[System.Serializable]
public class TemporalDataset
{
    public string model;
    public string field;
    public TemporalTimeStep[] timesteps;
}

public class TemporalInterpolationUnity : MonoBehaviour
{
    [Header("Dataset")]
    public TextAsset jsonDataset;

    [Header("Playback")]
    // FIX: simulationTime is now the single source of truth for which point
    // in the simulation is being displayed. Set it from the Inspector, a UI
    // slider, or another script. Range shown in Inspector matches dataset bounds.
    [Range(0f, 10f)]
    public float simulationTime = 0f;

    public bool isPlaying = true;
    public float playbackSpeed = 1f;   // multiplier — 1 = real time, 2 = double speed

    private TemporalDataset dataset;
    private float tMin;
    private float tMax;

    void Start()
    {
        if (jsonDataset == null)
        {
            Debug.LogError("[TemporalInterpolation] No JSON dataset assigned in the Inspector.");
            return;
        }

        dataset = JsonUtility.FromJson<TemporalDataset>(jsonDataset.text);

        if (dataset == null)
        {
            Debug.LogError("[TemporalInterpolation] Failed to parse JSON dataset.");
            return;
        }

        if (dataset.timesteps == null || dataset.timesteps.Length < 2)
        {
            Debug.LogError("[TemporalInterpolation] Dataset needs at least 2 timesteps.");
            dataset = null;
            return;
        }

        Array.Sort(dataset.timesteps, (a, b) => a.time.CompareTo(b.time));

        tMin = dataset.timesteps[0].time;
        tMax = dataset.timesteps[dataset.timesteps.Length - 1].time;

        simulationTime = tMin;
        Debug.Log($"[TemporalInterpolation] Loaded '{dataset.field}' | " +
                  $"{dataset.timesteps.Length} keyframes | t=[{tMin}, {tMax}]");
    }

    void Update()
    {
        if (dataset == null) return;

        // FIX: advance simulationTime by real delta, then loop back to tMin.
        // This replaces the old Time.time which couldn't be paused or scrubbed.
        if (isPlaying)
        {
            simulationTime += Time.deltaTime * playbackSpeed;
            if (simulationTime > tMax)
                simulationTime = tMin;
        }

        float[] interpolatedValues = InterpolateAtTime(simulationTime);

        if (interpolatedValues != null && interpolatedValues.Length > 0)
        {
            Debug.Log($"[TemporalInterpolation] {dataset.field} at " +
                      $"t={simulationTime:F2}: {interpolatedValues[0]:F4}...");
        }
    }

    float[] InterpolateAtTime(float queryTime)
    {
        // Clamp to dataset range
        if (queryTime <= tMin) return (float[])dataset.timesteps[0].values.Clone();
        if (queryTime >= tMax) return (float[])dataset.timesteps[dataset.timesteps.Length - 1].values.Clone();

        for (int i = 0; i < dataset.timesteps.Length - 1; i++)
        {
            TemporalTimeStep t0 = dataset.timesteps[i];
            TemporalTimeStep t1 = dataset.timesteps[i + 1];

            if (queryTime >= t0.time && queryTime <= t1.time)
            {
                float span = t1.time - t0.time;
                if (span <= 0f) return (float[])t0.values.Clone();

                float alpha = (queryTime - t0.time) / span;
                return InterpolateValues(t0.values, t1.values, alpha);
            }
        }

        return (float[])dataset.timesteps[0].values.Clone();
    }

    float[] InterpolateValues(float[] a, float[] b, float alpha)
    {
        int count = Mathf.Min(a.Length, b.Length);
        float[] result = new float[count];
        for (int i = 0; i < count; i++)
            result[i] = Mathf.Lerp(a[i], b[i], alpha);
        return result;
    }
}
