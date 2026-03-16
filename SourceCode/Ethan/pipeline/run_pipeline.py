
import os
from process_water import generate_heightmaps
from process_wind import process_wind_data
from process_turbine import process_turbine_data

INPUT_DIR = "../Simulation_Input"
UNITY_OUTPUT = "../Unity_Project/Assets/StreamingAssets"

def main():

    print("Starting simulation processing pipeline...")

    water_csv = os.path.join(INPUT_DIR, "water_surface.csv")
    wind_csv = os.path.join(INPUT_DIR, "wind_vectors.csv")
    turbine_csv = os.path.join(INPUT_DIR, "turbine_data.csv")

    heightmap_folder = os.path.join(UNITY_OUTPUT, "HeightMaps")
    wind_output = os.path.join(UNITY_OUTPUT, "Wind")
    turbine_output = os.path.join(UNITY_OUTPUT, "Turbine")

    generate_heightmaps(water_csv, heightmap_folder)
    process_wind_data(wind_csv, wind_output)
    process_turbine_data(turbine_csv, turbine_output)

    print("Pipeline complete. Unity assets generated.")

if __name__ == "__main__":
    main()