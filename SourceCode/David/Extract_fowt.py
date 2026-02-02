from paraview.simple import *
import numpy as np
import csv
import os

# --- SETTINGS ---
hub_coord = [7.5, 0.0, 2.135] # Your "Sweet Spot"
snapshots = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"]
foam_file = "case.foam"

# Create a dummy .foam file if it doesn't exist
if not os.path.exists(foam_file):
    with open(foam_file, "w") as f: pass

data_rows = []

print("--- Starting Extraction on Nobara Linux ---")

try:
    reader = OpenFOAMReader(FileName=foam_file)
    
    for snap in snapshots:
        reader.UpdatePipeline(float(snap))

        # 1. Extract Wind Speed
        probe = ProbeLocation(Input=reader)
        probe.ProbeType.Center = hub_coord
        probe.UpdatePipeline()
        u_vec = servermanager.Fetch(probe).GetPointData().GetArray("U").GetTuple(0)
        wind_speed = np.linalg.norm(u_vec)

        # 2. Extract Platform Motion (Heave and Pitch)
        platform = ExtractBlock(Input=reader)
        platform.Selectors = ['/Root/FOWT_platform'] 
        platform.UpdatePipeline()
        
        # Get Displacement Data
        p_data = servermanager.Fetch(platform).GetPointData()
        disp_array = p_data.GetArray("pointDisplacement")
        points_array = servermanager.Fetch(platform).GetPoints()
        
        # Math: Heave (Z) and Pitch (Tilt)
        num_points = disp_array.GetNumberOfTuples()
        disps = np.array([disp_array.GetTuple(i) for i in range(num_points)])
        coords = np.array([points_array.GetPoint(i) for i in range(num_points)])
        
        avg_heave = np.mean(disps[:, 2]) # ParaView Z
        avg_surge = np.mean(disps[:, 0]) # ParaView X
        
        # Calculate Pitch (Difference between front and back points)
        f_idx, b_idx = np.argmax(coords[:, 0]), np.argmin(coords[:, 0])
        pitch = np.degrees(np.arctan2(disps[f_idx, 2] - disps[b_idx, 2], coords[f_idx, 0] - coords[b_idx, 0]))

        data_rows.append([snap, wind_speed, avg_surge, avg_heave, pitch])
        print(f"Snap {snap}: Wind {wind_speed:.2f} | Heave {avg_heave:.4f} | Pitch {pitch:.4f}")

except Exception as e:
    print(f"Error: {e}")

# Save CSV
with open("Unity_FOWT_Data.csv", "w", newline='') as f:
    writer = csv.writer(f)
    writer.writerow(["Time", "WindSpeed", "Surge_X", "Heave_Z", "Pitch_Deg"])
    writer.writerows(data_rows)

print("\n--- Success! CSV Created ---")
