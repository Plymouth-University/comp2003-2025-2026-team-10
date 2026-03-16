from paraview.simple import *
import os

# --- CONFIG ---
snapshots = ["30.0", "31.0", "32.0", "33.0", "34.0", "35.0", "36.0", "37.0", "38.0", "39.0", "40.0"]
foam_file = "case.foam"
output_dir = "ExportedMeshes"

if not os.path.exists(output_dir): os.makedirs(output_dir)

reader = OpenFOAMReader(FileName=foam_file)
reader.UpdatePipelineInformation()

for snap in snapshots:
    t = float(snap)
    UpdatePipeline(time=t, proxy=reader)
    
    # 1. Extract the platform
    extract = ExtractBlock(Input=reader)
    extract.Selectors = ['/Root/FOWT_platform'] 
    
    # 2. Apply the 'Warp By Vector' to see the actual movement
    warp = WarpByVector(Input=extract)
    warp.Vectors = ['PointData', 'pointDisplacement']
    warp.ScaleFactor = 1.0
    
    # 3. Save as OBJ
    export_path = os.path.join(output_dir, f"platform_{snap}.obj")
    SaveData(export_path, proxy=warp)
    print(f"Exported: {export_path}")
    
    Delete(warp)
    Delete(extract)
