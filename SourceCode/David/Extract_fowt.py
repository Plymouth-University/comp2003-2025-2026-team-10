from paraview.simple import *
import numpy as np
import csv
import os

# --- SETTINGS ---
hub_coord = [7.5, 0.0, 2.135]
# Range set from 30 to 65 inclusive
snapshots = [str(i) for i in range(30, 66)]
foam_file = "case.foam"

# Create a dummy .foam file if it doesn't exist
if not os.path.exists(foam_file):
    with open(foam_file, "w") as f: pass

data_rows = []

print("--- Starting Wind Speed Extraction on CachyOS ---")
print(f"Processing snapshots: {snapshots[0]} to {snapshots[-1]}")

try:
    reader = OpenFOAMReader(FileName=foam_file)
    # Ensure Velocity (U) is enabled
    reader.PointArrays = ['U']

    for snap in snapshots:
        # Update the pipeline to the specific time step
        reader.UpdatePipeline(float(snap))

        # 1. Extract Wind Speed via Probe at Hub
        probe = ProbeLocation(Input=reader)
        probe.ProbeType.Center = hub_coord
        probe.UpdatePipeline()

        # Fetch the point data from the probe
        fetched_probe = servermanager.Fetch(probe)
        u_data = fetched_probe.GetPointData().GetArray("U")

        if u_data:
            u_vec = u_data.GetTuple(0)
            # Calculate magnitude (Wind Speed)
            wind_speed = np.linalg.norm(u_vec)
        else:
            wind_speed = 0.0
            print(f"Warning: No velocity data (U) found for Snap {snap}")

        data_rows.append([snap, wind_speed])
        print(f"Snap {snap}: Wind Speed {wind_speed:.4f}")

except Exception as e:
    print(f"Error during extraction: {e}")

# Save CSV - Simplified to Time and WindSpeed only
output_file = "Unity_Wind_Data.csv"
with open(output_file, "w", newline='') as f:
    writer = csv.writer(f)
    writer.writerow(["Time", "WindSpeed"])
    writer.writerows(data_rows)

print(f"\n--- Success! Data exported to {output_file} ---")
