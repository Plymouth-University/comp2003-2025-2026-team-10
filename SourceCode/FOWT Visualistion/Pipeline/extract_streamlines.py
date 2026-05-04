from paraview.simple import *
from vtkmodules.vtkCommonCore import vtkIdList
import numpy as np
import csv
import os

import sys
CASE_FILE  = sys.argv[1] if len(sys.argv) > 1 else 'case.foam'
OUTPUT_DIR = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Simulation_Input', 'wind')
NUM_SEEDS  = 100

os.makedirs(OUTPUT_DIR, exist_ok=True)

reader = OpenFOAMReader(FileName=CASE_FILE)
reader.PointArrays = ['U']
reader.UpdatePipeline()

scene = GetAnimationScene()
scene.UpdateAnimationUsingDataTimeSteps()
TIMESTEPS = [int(t) for t in scene.TimeKeeper.TimestepValues]
print(f"Found timesteps: {TIMESTEPS[0]} to {TIMESTEPS[-1]} ({len(TIMESTEPS)} total)")

tracer = StreamTracer(Input=reader, SeedType='Point Cloud')
tracer.Vectors = ['POINTS', 'U']
tracer.MaximumStreamlineLength = 12.0
tracer.IntegrationDirection = 'FORWARD'

seed = tracer.SeedType
seed.Center = [0.0, 0, 2.135]
seed.NumberOfPoints = NUM_SEEDS
seed.Radius = 1.96

for t in TIMESTEPS:
    scene.AnimationTime = float(t)
    tracer.UpdatePipeline(float(t))

    fetched = servermanager.Fetch(tracer)

    rows = []
    u_array = fetched.GetPointData().GetArray('U')

    lines = fetched.GetLines()
    lines.InitTraversal()

    streamline_id = 0
    id_list = vtkIdList()

    while lines.GetNextCell(id_list):
        for j in range(id_list.GetNumberOfIds()):
            pid = id_list.GetId(j)
            pos = fetched.GetPoint(pid)
            speed = np.linalg.norm(u_array.GetTuple(pid)) if u_array else 0.0
            rows.append([streamline_id, pos[0], pos[1], pos[2], speed])
        streamline_id += 1

    out_path = os.path.join(OUTPUT_DIR, f'streamline_{t}.csv')
    with open(out_path, 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(['id', 'x', 'y', 'z', 'speed'])
        writer.writerows(rows)

    print(f'Timestep {t}: {streamline_id} streamlines, {len(rows)} points')

print('Done.')
