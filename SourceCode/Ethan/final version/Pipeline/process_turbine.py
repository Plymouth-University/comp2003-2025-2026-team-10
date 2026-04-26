import os
import shutil

REQUIRED_COLUMNS = {"Time", "WindSpeed", "Surge_X", "Heave_Z", "Pitch_Deg"}


def process_turbine_data(input_folder, output_folder):
    print("Processing turbine data...")

    csv_files = [f for f in os.listdir(input_folder) if f.endswith(".csv")]

    if not csv_files:
        raise FileNotFoundError(f"No CSV files found in: {input_folder}")

    if len(csv_files) > 1:
        print(f"  Warning: multiple CSVs found, using first: {csv_files[0]}")

    src = os.path.join(input_folder, csv_files[0])

    with open(src) as f:
        header_cols = {c.strip() for c in f.readline().split(",")}

    missing = REQUIRED_COLUMNS - header_cols
    if missing:
        raise ValueError(f"Turbine CSV missing columns: {missing}")

    dst = os.path.join(output_folder, "turbine.csv")
    shutil.copy2(src, dst)
    print(f"  Turbine data written to {dst}")
