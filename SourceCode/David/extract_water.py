import os
from paraview.simple import *
import paraview.servermanager as sm

# --- CONFIGURATION ---
data_directory = "./"
output_directory = "./Unity_Water_OBJ"
water_var = "alpha.water"
mesh_resolution = 127

if not os.path.exists(output_directory):
    os.makedirs(output_directory)

all_folders = os.listdir(data_directory)
time_folders = sorted([f for f in all_folders if f.isdigit()], key=int)

print(f"Found {len(time_folders)} frames. Exporting to Unity-native OBJ...")

# --- INITIALIZE ---
reader = OpenFOAMReader(FileName=os.path.join(data_directory, "case.foam"))
reader.MeshRegions = ['internalMesh']
reader.CellArrays = [water_var]

# 1. CREATE THE BLANKET (Matching your bounds)
ocean_blanket = Plane()
ocean_blanket.XResolution = mesh_resolution
ocean_blanket.YResolution = mesh_resolution
ocean_blanket.Origin = [0.0, -7.75, 0.0]
ocean_blanket.Point1 = [15.0, -7.75, 0.0]
ocean_blanket.Point2 = [0.0, 7.75, 0.0]

# --- THE LOOP ---
for index, folder in enumerate(time_folders):
    t_val = float(folder)
    reader.UpdatePipeline(t_val)

    # A. Convert Cells to Points
    point_data = CellDatatoPointData(Input=reader)

    # B. Probe the data
    probed_data = sm.filters.ResampleWithDataset()
    try:
        probed_data.SourceDataArrays = point_data
        probed_data.DestinationMesh = ocean_blanket
    except AttributeError:
        probed_data.Source = point_data
        probed_data.Input = ocean_blanket

    # C. WARP THE MESH (This makes the waves physical)
    warped_mesh = WarpByScalar(Input=probed_data, Scalars=water_var)
    warped_mesh.ScaleFactor = 2.0  # Increase this if the waves look too flat

    # D. SAVE AS OBJ
    # Note: OBJ doesn't support time sequences in one file, so we save individual frames
    out_file = os.path.join(output_directory, f"water_{index:03d}.obj")
    SaveData(out_file, proxy=warped_mesh)

    print(f"Exported OBJ: {out_file}")

    # Cleanup to keep memory low
    Delete(warped_mesh)
    Delete(probed_data)
    Delete(point_data)

print("\nSuccess! Drag the 'Unity_Water_OBJ' folder into your Unity project.")
