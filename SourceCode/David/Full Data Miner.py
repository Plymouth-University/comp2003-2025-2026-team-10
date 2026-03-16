from paraview.simple import *
import csv
import numpy as np

# --- CONFIG ---
hub_coord = [7.5, 0.0, 2.135]
# Use the snapshots detected previously on your Bazzite setup
snapshots = ["30.0", "31.0", "32.0", "33.0", "34.0", "35.0", "36.0", "37.0", "38.0", "39.0", "40.0"]
foam_file = "case.foam"
platform_patch_name = "FOWT_platform" # Change this to match your boundary name

def extract_all():
    reader = OpenFOAMReader(FileName=foam_file)
    reader.CaseType = 'Decomposed Case'
    reader.UpdatePipelineInformation()
    
    data_rows = []
    unity_time = 0.0

    for snap in snapshots:
        t = float(snap)
        UpdatePipeline(time=t, proxy=reader)

        # A. WIND SPEED (Hub Point)
        probe = ProbeLocation(Input=reader)
        probe.ProbeType.Center = hub_coord
        probe.UpdatePipeline(time=t)
        u_array = servermanager.Fetch(probe).GetPointData().GetArray("U")
        wind_speed = np.linalg.norm(u_array.GetTuple(0)) if u_array else 0.0

        # B. HEAVE & PITCH (Platform Mesh)
        # Extract only the platform patch
        extract = ExtractBlock(Input=reader)
        # Note: Selectors vary by ParaView version; using a general search if possible
        extract.Selectors = [f'/Root/{platform_patch_name}'] 
        extract.UpdatePipeline(time=t)
        
        block_data = servermanager.Fetch(extract)
        # Unwrap MultiBlock if necessary
        if block_data.IsA("vtkMultiBlockDataSet"):
            block_data = block_data.GetBlock(0)

        heave = 0.0
        pitch = 0.0

        if block_data:
            disp_array = block_data.GetPointData().GetArray("pointDisplacement")
            pts = block_data.GetPoints()
            
            if disp_array and pts:
                # Convert to numpy for math
                num_pts = disp_array.GetNumberOfTuples()
                disps = np.array([disp_array.GetTuple(i) for i in range(num_pts)])
                coords = np.array([pts.GetPoint(i) for i in range(num_pts)])

                # HEAVE: Average of Z-displacements
                heave = np.mean(disps[:, 2])

                # PITCH: Difference in Z-disp between most forward (X+) and most aft (X-) points
                fwd_idx = np.argmax(coords[:, 0])
                aft_idx = np.argmin(coords[:, 0])
                
                dz = disps[fwd_idx, 2] - disps[aft_idx, 2]
                dx = coords[fwd_idx, 0] - coords[aft_idx, 0]
                # tan(theta) = dz/dx
                pitch = np.degrees(np.arctan2(dz, dx))

        print(f"Time {snap}: Wind {wind_speed:.2f} | Heave {heave:.3f} | Pitch {pitch:.3f}")
        data_rows.append([unity_time, wind_speed, heave, pitch])
        unity_time += 1.0
        Delete(probe)
        Delete(extract)

    with open("TurbineData.csv", "w", newline='') as f:
        writer = csv.writer(f)
        writer.writerow(["Time", "WindSpeed", "Heave", "Pitch"])
        writer.writerows(data_rows)

extract_all()
