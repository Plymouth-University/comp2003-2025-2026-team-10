"""
extract_all.py  —  run with:  pvpython extract_all.py

Opens the OpenFOAM case once and extracts all data types per timestep:
  water surface          →  Simulation_Input/water/wave_N.csv
  wind streamlines       →  Simulation_Input/wind/N.csv
  turbine motion         →  Simulation_Input/turbine/turbine.csv
  pressure field         →  Simulation_Input/pressure/pressure_N.csv
  vorticity field        →  Simulation_Input/vorticity/vorticity_N.csv
  platform pressure      →  Simulation_Input/platform_pressure/platformpressure_N.csv

Output feeds directly into run_pipeline.py.
"""

from paraview.simple import *
import numpy as np
import csv
import os

# ─────────────────────────────────────────────────────────────
# CONFIG — edit these before running
CASE_FILE    = "case.foam"               # path to your .foam file
OUTPUT_DIR   = "./Simulation_Input"      # output root (fed into run_pipeline.py)
CASE_TYPE    = "Decomposed Case"         # "Decomposed Case" for parallel runs,
                                         # "Reconstructed Case" for single proc
TIMESTEPS    = [30.0, 31.0, 32.0, 33.0, 34.0, 35.0, 36.0, 37.0, 38.0, 39.0, 40.0,41.0,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65]  # list of timesteps to extract (must match your case)

# turbine settings
HUB_COORDS       = [7.5, 0.0, 2.135]
PLATFORM_BLOCK   = "/Root/FOWT_platform"

# wind streamline settings
SEED_CENTER      = [7.875, 0.029, 2.127]
SEED_RADIUS      = 1.7
SEED_POINTS      = 150
MAX_PROPAGATION  = 15.5
MAX_STEPS        = 5000
# ─────────────────────────────────────────────────────────────

WATER_DIR             = os.path.join(OUTPUT_DIR, "water")
WIND_DIR              = os.path.join(OUTPUT_DIR, "wind")
TURBINE_DIR           = os.path.join(OUTPUT_DIR, "turbine")
PRESSURE_DIR          = os.path.join(OUTPUT_DIR, "pressure")
VORTICITY_DIR         = os.path.join(OUTPUT_DIR, "vorticity")
PLATFORM_PRESSURE_DIR = os.path.join(OUTPUT_DIR, "platform_pressure")

for d in [WATER_DIR, WIND_DIR, TURBINE_DIR, PRESSURE_DIR, VORTICITY_DIR, PLATFORM_PRESSURE_DIR]:
    os.makedirs(d, exist_ok=True)


def open_case():
    reader = OpenFOAMReader(FileName=CASE_FILE)
    reader.CaseType = CASE_TYPE
    reader.MeshRegions = ["internalMesh", "FOWT_platform"]
    reader.CellArrays  = ["U", "p", "alpha.water"]
    reader.PointArrays = ["U", "pointDisplacement"]
    return reader


def extract_water(reader, timestep_index):
    """Contour alpha.water=0.5 isosurface → CSV with Points:0/1/2."""
    contour = Contour(Input=reader)
    contour.ContourBy  = ["POINTS", "alpha.water"]
    contour.Isosurfaces = [0.5]
    contour.ComputeNormals = 0
    contour.ComputeScalars = 1
    contour.UpdatePipeline()

    out_path = os.path.join(WATER_DIR, f"wave_{timestep_index}.csv")
    SaveData(out_path, proxy=contour)
    Delete(contour)
    return out_path


def extract_wind(reader, timestep_index):
    """StreamTracer on U field → CSV with RegionId, Points:0/1/2."""
    tracer = StreamTracer(Input=reader)
    tracer.SeedType               = "Point Source"
    tracer.SeedType.Center        = SEED_CENTER
    tracer.SeedType.Radius        = SEED_RADIUS
    tracer.SeedType.NumberOfPoints = SEED_POINTS
    tracer.Vectors                = ["POINTS", "U"]
    tracer.IntegrationDirection   = "BOTH"
    tracer.IntegratorType         = "Runge-Kutta 4-5"
    tracer.MaximumStreamlineLength = MAX_PROPAGATION
    tracer.MaximumNumberOfSteps   = MAX_STEPS
    tracer.MaximumError           = 1e-6
    tracer.InitialIntegrationStep = 0.02
    tracer.MinimumIntegrationStep = 0.01
    tracer.MaximumIntegrationStep = 0.5
    tracer.UpdatePipeline()

    connected = Connectivity(Input=tracer)
    connected.UpdatePipeline()

    poly       = servermanager.Fetch(connected)
    pts        = poly.GetPoints()
    region_arr = poly.GetPointData().GetArray("RegionId")
    n_pts      = pts.GetNumberOfPoints()

    out_path = os.path.join(WIND_DIR, f"{timestep_index}.csv")
    with open(out_path, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["RegionId", "Points:0", "Points:1", "Points:2"])
        for j in range(n_pts):
            x, y, z = pts.GetPoint(j)
            rid = int(region_arr.GetValue(j)) if region_arr else 0
            writer.writerow([rid, x, y, z])

    Delete(connected)
    Delete(tracer)
    return out_path


def extract_turbine(reader, timestep):
    """Probe wind speed at hub + extract platform displacement."""
    # wind speed at hub
    probe = ProbeLocation(Input=reader)
    probe.ProbeType.Center = HUB_COORDS
    probe.UpdatePipeline()
    u_vec      = servermanager.Fetch(probe).GetPointData().GetArray("U").GetTuple(0)
    wind_speed = float(np.linalg.norm(u_vec))
    Delete(probe)

    # platform displacement
    platform = ExtractBlock(Input=reader)
    platform.Selectors = [PLATFORM_BLOCK]
    platform.UpdatePipeline()
    fetched    = servermanager.Fetch(platform)
    disp_arr   = fetched.GetPointData().GetArray("pointDisplacement")
    coords_arr = fetched.GetPoints()
    n          = disp_arr.GetNumberOfTuples()

    disps  = np.array([disp_arr.GetTuple(k)       for k in range(n)])
    coords = np.array([coords_arr.GetPoint(k)      for k in range(n)])

    avg_heave = float(np.mean(disps[:, 2]))
    avg_surge = float(np.mean(disps[:, 0]))

    f_idx = int(np.argmax(coords[:, 0]))
    b_idx = int(np.argmin(coords[:, 0]))
    pitch = float(np.degrees(np.arctan2(
        disps[f_idx, 2] - disps[b_idx, 2],
        coords[f_idx, 0] - coords[b_idx, 0]
    )))

    Delete(platform)
    return wind_speed, avg_surge, avg_heave, pitch


def extract_pressure(reader, timestep_index):
    """Contour alpha.water=0.5 isosurface → CSV with Points:0/1/2 and p."""
    contour = Contour(Input=reader)
    contour.ContourBy  = ["POINTS", "alpha.water"]
    contour.Isosurfaces = [0.5]
    contour.ComputeNormals = 0
    contour.ComputeScalars = 1
    contour.UpdatePipeline()

    poly  = servermanager.Fetch(contour)
    pts   = poly.GetPoints()
    p_arr = poly.GetPointData().GetArray("p")
    n_pts = pts.GetNumberOfPoints()

    out_path = os.path.join(PRESSURE_DIR, f"pressure_{timestep_index}.csv")
    with open(out_path, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["Points:0", "Points:1", "Points:2", "p"])
        for j in range(n_pts):
            x, y, z = pts.GetPoint(j)
            p = float(p_arr.GetValue(j)) if p_arr else 0.0
            writer.writerow([x, y, z, p])

    Delete(contour)
    return out_path


def extract_vorticity(reader, timestep_index):
    """Compute vorticity magnitude on alpha.water=0.5 isosurface → CSV with Points:0/1/2 and vorticity_mag."""
    # Compute vorticity vector field from U using ParaView's built-in filter
    vorticity = ComputeDerivatives(Input=reader)
    vorticity.Scalars          = ["POINTS", ""]
    vorticity.Vectors          = ["POINTS", "U"]
    vorticity.ComputeVorticity = 1
    vorticity.UpdatePipeline()

    # Sample vorticity on the water surface isosurface
    contour = Contour(Input=vorticity)
    contour.ContourBy   = ["POINTS", "alpha.water"]
    contour.Isosurfaces = [0.5]
    contour.ComputeNormals = 0
    contour.ComputeScalars = 1
    contour.UpdatePipeline()

    poly     = servermanager.Fetch(contour)
    pts      = poly.GetPoints()
    # Try all known array name variants across ParaView versions —
    # the output array name from ComputeDerivatives varies between versions. - jenson
    vort_arr = poly.GetPointData().GetArray("Vorticity")
    if vort_arr is None:
        vort_arr = poly.GetPointData().GetArray("Vorticity Vector")
    if vort_arr is None:
        vort_arr = poly.GetPointData().GetArray("vorticity")
    n_pts    = pts.GetNumberOfPoints()

    out_path = os.path.join(VORTICITY_DIR, f"vorticity_{timestep_index}.csv")
    with open(out_path, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["Points:0", "Points:1", "Points:2", "vorticity_mag"])
        for j in range(n_pts):
            x, y, z = pts.GetPoint(j)
            if vort_arr:
                vx, vy, vz = vort_arr.GetTuple(j)
                mag = float(np.sqrt(vx*vx + vy*vy + vz*vz))
            else:
                mag = 0.0
            writer.writerow([x, y, z, mag])

    Delete(contour)
    Delete(vorticity)
    return out_path


def extract_platform_pressure(reader, timestep_index):
    """Extract pressure on FOWT platform surface → CSV with Points:0/1/2 and p."""
    # Extract the platform block which is already loaded by the reader
    platform = ExtractBlock(Input=reader)
    platform.Selectors = [PLATFORM_BLOCK]
    platform.UpdatePipeline()

    fetched = servermanager.Fetch(platform)
    pts     = fetched.GetPoints()
    p_arr   = fetched.GetPointData().GetArray("p")
    n_pts   = pts.GetNumberOfPoints()

    out_path = os.path.join(PLATFORM_PRESSURE_DIR, f"platformpressure_{timestep_index}.csv")
    with open(out_path, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["Points:0", "Points:1", "Points:2", "p"])
        for j in range(n_pts):
            x, y, z = pts.GetPoint(j)
            p = float(p_arr.GetValue(j)) if p_arr else 0.0
            writer.writerow([x, y, z, p])

    Delete(platform)
    return out_path


# ── main ──────────────────────────────────────────────────────

print("=== extract_all.py ===")
print(f"Case:      {CASE_FILE}")
print(f"Timesteps: {TIMESTEPS}")
print(f"Output:    {OUTPUT_DIR}\n")

reader = open_case()
turbine_rows = []

for i, t in enumerate(TIMESTEPS):
    print(f"[{i+1}/{len(TIMESTEPS)}] t={t}")
    reader.UpdatePipeline(t)

    water_path = extract_water(reader, i)
    print(f"  water              → {water_path}")

    wind_path = extract_wind(reader, i)
    print(f"  wind               → {wind_path}")

    pressure_path = extract_pressure(reader, i)
    print(f"  pressure           → {pressure_path}")

    vorticity_path = extract_vorticity(reader, i)
    print(f"  vorticity          → {vorticity_path}")

    platform_pressure_path = extract_platform_pressure(reader, i)
    print(f"  platform pressure  → {platform_pressure_path}")

    wind_speed, surge, heave, pitch = extract_turbine(reader, t)
    turbine_rows.append([float(i + 1), wind_speed, surge, heave, pitch])
    print(f"  turbine: wind={wind_speed:.2f}  surge={surge:.4f}  heave={heave:.4f}  pitch={pitch:.4f}")

turbine_path = os.path.join(TURBINE_DIR, "turbine.csv")
with open(turbine_path, "w", newline="") as f:
    writer = csv.writer(f)
    writer.writerow(["Time", "WindSpeed", "Surge_X", "Heave_Z", "Pitch_Deg"])
    writer.writerows(turbine_rows)

print(f"\n  turbine            → {turbine_path}")
print("\n=== Done. Run: python run_pipeline.py ===")
