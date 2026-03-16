// Does not necessarily work with unity prototype as of now 
/* The purpose of this is to demonstrate
 loading keyframes from JSON, and reconstruct
 this prototype focuses on data flow and reconstruction logic, not
rendering or UI.
   */
using System;
using UnityEngine;

//the purpose of the prototype is to Load temporal keyframes from json
// reconstruct fframes using LERP
// Focus on data flow and reconstruction logic


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
    [Header("Temporal Interpolation Dataset")]
    public TextAsset jsonDataset;

    private TemporalDataset dataset;

    void Start()
    {
        if (jsonDataset == null)
        {
            Debug.LogError("No JSON dataset assigned.");
            return;
        }

        dataset = JsonUtility.FromJson<TemporalDataset>(jsonDataset.text);

        if (dataset == null || dataset.timesteps == null || dataset.timesteps.Length < 2)
        {
            Debug.LogError("Invalid temporal interpolation dataset.");
            dataset = null;
            return;
        }

        Array.Sort(dataset.timesteps, (a, b) => a.time.CompareTo(b.time));
    }

    void Update()
    {
        if (dataset == null) return;

        float tMin = dataset.timesteps[0].time;
        float tMax = dataset.timesteps[dataset.timesteps.Length - 1].time;
        float duration = Mathf.Max(0.0001f, tMax - tMin);

        float queryTime = tMin + Mathf.Repeat(Time.time, duration);

        float[] interpolatedValues = InterpolateAtTime(queryTime);

        if (interpolatedValues != null && interpolatedValues.Length > 0)
        {
            Debug.Log($"Interpolated {dataset.field} at t={queryTime:F2}: {interpolatedValues[0]:F3}...");
        }
    }

    float[] InterpolateAtTime(float queryTime)
    {
        for (int i = 0; i < dataset.timesteps.Length - 1; i++)
        {
            TemporalTimeStep t0 = dataset.timesteps[i];
            TemporalTimeStep t1 = dataset.timesteps[i + 1];

            if (queryTime >= t0.time && queryTime <= t1.time)
            {
                float span = t1.time - t0.time;
                if (span <= 0f) return t0.values;

                float alpha = (queryTime - t0.time) / span;
                return InterpolateValues(t0.values, t1.values, alpha);
            }
        }

        return dataset.timesteps[0].values;
    }

    float[] InterpolateValues(float[] a, float[] b, float alpha)
    {
        int count = Mathf.Min(a.Length, b.Length);
        float[] result = new float[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = Mathf.Lerp(a[i], b[i], alpha);
        }

        return result;
    }
}