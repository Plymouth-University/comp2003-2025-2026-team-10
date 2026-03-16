// SimulationClock.cs
// ===================
// Shared simulation clock that drives both WaveMeshDeformer and
// StreamlineSystem from the same timestep, keeping waves and wind in sync.
//
// Setup in Unity:
//   1. Create an empty GameObject called "SimulationManager".
//   2. Attach this script to it.
//   3. Drag this GameObject into the "clock" field on WaveMeshDeformer.
//   4. Set frameCount to match how many CSV timesteps you have.
//   5. Optionally connect a UI Slider to ScrubTo() for manual scrubbing.

using UnityEngine;
using UnityEngine.Events;

public class SimulationClock : MonoBehaviour
{
    [Header("Playback")]
    [Tooltip("Total number of timestep frames loaded (set to match your CSV count).")]
    public int frameCount = 1;

    [Tooltip("Frames per second of simulation playback.")]
    public float playbackFPS = 10f;

    public bool isPlaying = true;
    public bool loop = true;

    [Header("State (read-only in Inspector)")]
    [SerializeField] private int currentFrameIndex = 0;
    [SerializeField] private float accumulator = 0f;

    [Header("Events")]
    [Tooltip("Fires every time the frame index changes. Wire up any listeners here.")]
    public UnityEvent<int> onFrameChanged;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Current frame index, clamped to [0, frameCount-1].</summary>
    public int CurrentFrameIndex => currentFrameIndex;

    /// <summary>Normalised playback position 0..1.</summary>
    public float NormalisedTime => frameCount > 1
        ? (float)currentFrameIndex / (frameCount - 1)
        : 0f;

    /// <summary>Scrub to a specific frame. Useful for UI sliders.</summary>
    public void ScrubTo(int frame)
    {
        int clamped = Mathf.Clamp(frame, 0, Mathf.Max(0, frameCount - 1));
        if (clamped == currentFrameIndex) return;
        currentFrameIndex = clamped;
        accumulator = 0f;
        onFrameChanged?.Invoke(currentFrameIndex);
    }

    /// <summary>Scrub using a normalised 0..1 value (e.g. from a UI Slider).</summary>
    public void ScrubNormalised(float t)
    {
        ScrubTo(Mathf.RoundToInt(t * (frameCount - 1)));
    }

    public void Play()  => isPlaying = true;
    public void Pause() => isPlaying = false;

    public void StepForward()  => ScrubTo(currentFrameIndex + 1);
    public void StepBackward() => ScrubTo(currentFrameIndex - 1);

    // -------------------------------------------------------------------------

    void Update()
    {
        if (!isPlaying || frameCount <= 1) return;

        accumulator += Time.deltaTime * playbackFPS;

        while (accumulator >= 1f)
        {
            accumulator -= 1f;
            int next = currentFrameIndex + 1;

            if (next >= frameCount)
            {
                if (loop) next = 0;
                else
                {
                    next = frameCount - 1;
                    isPlaying = false;
                }
            }

            if (next != currentFrameIndex)
            {
                currentFrameIndex = next;
                onFrameChanged?.Invoke(currentFrameIndex);
            }
        }
    }

    void OnValidate()
    {
        // Keep Inspector values sane while editing
        frameCount = Mathf.Max(1, frameCount);
        playbackFPS = Mathf.Max(0.1f, playbackFPS);
        currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, Mathf.Max(0, frameCount - 1));
    }
}
