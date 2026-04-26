import os
import shutil


def process_wind_data(input_folder, output_folder):
    print("Processing wind streamline data...")

    csv_files = sorted([f for f in os.listdir(input_folder) if f.endswith(".csv")])

    if not csv_files:
        raise FileNotFoundError(f"No CSV files found in: {input_folder}")

    # clear old CSVs so stale files don't mix with new ones
    for existing in os.listdir(output_folder):
        if existing.endswith(".csv"):
            os.remove(os.path.join(output_folder, existing))

    for i, filename in enumerate(csv_files):
        src = os.path.join(input_folder, filename)
        dst = os.path.join(output_folder, f"{i}.csv")
        shutil.copy2(src, dst)
        print(f"  {filename} → {i}.csv")

    print(f"Wind data done: {len(csv_files)} files written to {output_folder}")
