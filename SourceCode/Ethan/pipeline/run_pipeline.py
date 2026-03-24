import os
import json
from process_water import generate_heightmaps
from process_wind import process_wind_data
from process_turbine import process_turbine_data

INPUT_DIR = "../Simulation_Input"
UNITY_OUTPUT = "../Unity_Project/Assets/StreamingAssets"

WATER_INPUT = os.path.join(INPUT_DIR, "water")
WIND_INPUT = os.path.join(INPUT_DIR, "wind")
TURBINE_INPUT = os.path.join(INPUT_DIR, "turbine")

HEIGHTMAP_OUTPUT = os.path.join(UNITY_OUTPUT, "HeightMaps")
WIND_OUTPUT = os.path.join(UNITY_OUTPUT, "Wind")
TURBINE_OUTPUT = os.path.join(UNITY_OUTPUT, "Turbine")

METADATA_PATH = os.path.join(UNITY_OUTPUT, "simulation.json")


def ensure_dirs():
    os.makedirs(HEIGHTMAP_OUTPUT, exist_ok=True)
    os.makedirs(WIND_OUTPUT, exist_ok=True)
    os.makedirs(TURBINE_OUTPUT, exist_ok=True)


def count_frames(folder):
    return len([f for f in os.listdir(folder) if f.endswith(".csv")])


def write_metadata(frame_count, grid_size):
    metadata = {
        "frameCount": frame_count,
        "fps": 10,
        "gridSize": grid_size,
        "heightScale": 2.0
    }

    with open(METADATA_PATH, "w") as f:
        json.dump(metadata, f, indent=4)


def main():
    print("=== Simulation Pipeline Start ===")

    ensure_dirs()

    if not os.path.exists(WATER_INPUT):
        raise Exception("Missing water input folder")

    frame_count = count_frames(WATER_INPUT)

    print(f"Detected {frame_count} water frames")

    grid_size = generate_heightmaps(
        input_folder=WATER_INPUT,
        output_folder=HEIGHTMAP_OUTPUT
    )

    process_wind_data(WIND_INPUT, WIND_OUTPUT)
    process_turbine_data(TURBINE_INPUT, TURBINE_OUTPUT)

    write_metadata(frame_count, grid_size)

    print("=== Pipeline Complete ===")


if __name__ == "__main__":
    main()