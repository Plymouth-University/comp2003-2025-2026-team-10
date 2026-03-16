import json

# FIX: export now matches the 3-keyframe structure in temporal_interpolation_example.json.
# The old version only exported 2 keyframes, which would have overwritten the better
# 3-keyframe JSON and caused temporal_interpolation.py to fail on t=1.5.

data = {
    "model": "temporal_interpolation",
    "field": "velocity_y",
    "timesteps": [
        # Keyframe 1 — initial state
        # These values are extracted from the CFD dataset at t=0.0
        {"time": 0.0, "values": [0.12, 0.15, 0.18, 0.20]},

        # Keyframe 2 — mid simulation
        {"time": 1.0, "values": [0.22, 0.25, 0.27, 0.30]},

        # Keyframe 3 — later state
        # Storing sparse keyframes rather than every timestep
        # is what reduces total dataset size.
        # Runtime interpolation (LERP) reconstructs in-between frames.
        # See temporal_interpolation.py and TemporalInterpolationUnity.cs.
        {"time": 2.0, "values": [0.28, 0.32, 0.35, 0.37]},
    ]
}

with open("temporal_interpolation_example.json", "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2)

print("Export updated: temporal_interpolation_example.json")
