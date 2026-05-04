from paraview.simple import *
import csv
import os
import sys

# --- SETTINGS ---
CASE_FILE  = sys.argv[1] if len(sys.argv) > 1 else 'case.foam'
OUTPUT_DIR = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Simulation_Input', 'water')

os.makedirs(OUTPUT_DIR, exist_ok=True)

print("--- Starting Water Surface Extraction ---")

reader = OpenFOAMReader(FileName=CASE_FILE)
reader.MeshRegions = ['internalMesh']
reader.CellArrays  = ['alpha.water']
reader.UpdatePipeline()

scene = GetAnimationScene()
scene.UpdateAnimationUsingDataTimeSteps()
TIMESTEPS = [int(t) for t in scene.TimeKeeper.TimestepValues]
print(f"Found timesteps: {TIMESTEPS[0]} to {TIMESTEPS[-1]} ({len(TIMESTEPS)} total)")

for t in TIMESTEPS:
    scene.AnimationTime = float(t)
    reader.UpdatePipeline(float(t))

    contour = Contour(Input=reader)
    contour.ContourBy    = ['POINTS', 'alpha.water']
    contour.Isosurfaces  = [0.5]
    contour.ComputeNormals = 0
    contour.UpdatePipeline()

    fetched = servermanager.Fetch(contour)
    pts     = fetched.GetPoints()
    n_pts   = pts.GetNumberOfPoints()

    out_path = os.path.join(OUTPUT_DIR, f'wave_{t}.csv')
    with open(out_path, 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(['Points:0', 'Points:1', 'Points:2'])
        for j in range(n_pts):
            x, y, z = pts.GetPoint(j)
            writer.writerow([x, y, z])

    Delete(contour)
    print(f'Timestep {t}: {n_pts} surface points → {out_path}')

print('\n--- Water extraction done ---')
