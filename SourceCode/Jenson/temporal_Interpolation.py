import json

data = {
    "model": "temporal_interpolation",
    "field": "velocity_y",
    "timesteps": [
        # this by process of time intervals extract from a CFD dataset
        # meaning timesteps are captured to shrink total data size
        {"time": 0.0, "values": [0.12, 0.15, 0.18]},

        #this denotes following subsequent keyframes
        #further clarification can be found in the design document, and the README.
        {"time": 1.0, "values": [0.20, 0.22, 0.25]}
        
    ]
}

with open("temporal_interpolation_example.json", "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2)

print("JSON updated")