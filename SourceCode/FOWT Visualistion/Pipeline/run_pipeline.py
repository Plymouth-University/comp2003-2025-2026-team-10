import os
import json
from process_water import generate_heightmaps
from process_wind import process_wind_data
from process_turbine import process_turbine_data

# Paths are resolved relative to this script — no editing needed
_HERE         = os.path.dirname(os.path.abspath(__file__))
INPUT_DIR     = os.path.join(_HERE, "Simulation_Input")
UNITY_PROJECT = os.path.join(_HERE, "Unity Project")

WATER_INPUT    = os.path.join(INPUT_DIR, "water")
WIND_INPUT     = os.path.join(INPUT_DIR, "wind")
TURBINE_INPUT  = os.path.join(INPUT_DIR, "turbine")

STREAMING      = os.path.join(UNITY_PROJECT, "Assets", "StreamingAssets")
HEIGHTMAP_OUT  = os.path.join(STREAMING, "HeightMaps")
TURBINE_OUT    = os.path.join(STREAMING, "Turbine")
WIND_OUT       = os.path.join(UNITY_PROJECT, "Assets", "Resources", "Streamline")
METADATA_FILE  = os.path.join(STREAMING, "simulation.json")


def ensure_dirs():
    for d in [HEIGHTMAP_OUT, TURBINE_OUT, WIND_OUT]:
        os.makedirs(d, exist_ok=True)


def count_frames(folder):
    return len([f for f in os.listdir(folder) if f.endswith(".csv")])


def write_metadata(frame_count, grid_size):
    with open(METADATA_FILE, "w") as f:
        json.dump({
            "frameCount": frame_count,
            "fps": 10,
            "gridSize": grid_size,
            "heightScale": 2.0
        }, f, indent=2)
    print("Wrote metadata:", METADATA_FILE)


def main():
    print("=== CFD Pipeline ===")
    ensure_dirs()

    grid_size = generate_heightmaps(WATER_INPUT, HEIGHTMAP_OUT)
    frame_count = count_frames(WATER_INPUT)

    process_wind_data(WIND_INPUT, WIND_OUT)
    process_turbine_data(TURBINE_INPUT, TURBINE_OUT)

    write_metadata(frame_count, grid_size)
    print("=== Done — open Unity and hit Play ===")


if __name__ == "__main__":
    main()
