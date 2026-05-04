from paraview.simple import *
import numpy as np
import csv
import os

# --- SETTINGS ---
import sys
hub_coord   = [7.5, 0.0, 2.135]
foam_file   = sys.argv[1] if len(sys.argv) > 1 else 'case.foam'
output_file = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Simulation_Input', 'turbine', 'wind_data.csv')

# Create a dummy .foam file if it doesn't exist
if not os.path.exists(foam_file):
    with open(foam_file, "w") as f: pass

data_rows = []
print("--- Starting Wind Speed Extraction ---")

try:
    reader = OpenFOAMReader(FileName=foam_file)
    reader.PointArrays = ['U']
    reader.UpdatePipeline()

    # Auto-detect timesteps
    scene = GetAnimationScene()
    scene.UpdateAnimationUsingDataTimeSteps()
    snapshots = [str(int(t)) for t in scene.TimeKeeper.TimestepValues]
    print(f"Found timesteps: {snapshots[0]} to {snapshots[-1]} ({len(snapshots)} total)")

    for snap in snapshots:
        reader.UpdatePipeline(float(snap))

        probe = ProbeLocation(Input=reader)
        probe.ProbeType.Center = hub_coord
        probe.UpdatePipeline()

        fetched_probe = servermanager.Fetch(probe)
        u_data = fetched_probe.GetPointData().GetArray("U")

        if u_data:
            u_vec = u_data.GetTuple(0)
            wind_speed = np.linalg.norm(u_vec)
        else:
            wind_speed = 0.0
            print(f"Warning: No velocity data found for Snap {snap}")

        data_rows.append([snap, wind_speed])
        print(f"Snap {snap}: Wind Speed {wind_speed:.4f}")

except Exception as e:
    print(f"Error during extraction: {e}")

with open(output_file, "w", newline='') as f:
    writer = csv.writer(f)
    writer.writerow(["Time", "WindSpeed"])
    writer.writerows(data_rows)

print(f"\n--- Success! Data exported to {output_file} ---")
