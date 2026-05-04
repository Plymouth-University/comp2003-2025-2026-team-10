"""
run_pipeline.py — single entry point for the FOWT visualisation pipeline.

Edit the CONFIG block below, then run:
    python run_pipeline.py

If Simulation_Input already contains extracted CSVs the ParaView extraction
step is skipped automatically — useful when re-running after tweaking settings.
"""

import os
import subprocess
import sys

# ─────────────────────────────────────────────────────────────────────────────
# CONFIG — edit these two lines before running
CASE_FILE = "C:/path/to/case.foam"   # full path to your .foam file
PVPYTHON  = "pvpython"               # path to pvpython if not on system PATH
# ─────────────────────────────────────────────────────────────────────────────

_HERE = os.path.dirname(os.path.abspath(__file__))

# Intermediate working data
SIM_INPUT    = os.path.join(_HERE, "Simulation_Input")
WATER_INPUT  = os.path.join(SIM_INPUT, "water")
WIND_INPUT   = os.path.join(SIM_INPUT, "wind")
TURBINE_INPUT = os.path.join(SIM_INPUT, "turbine")

# Intermediate heightmaps folder (fed into optical_flow.py)
HEIGHTMAP_WORK = os.path.join(SIM_INPUT, "water", "heightmaps")

# Final output — David's built Unity app
STREAMING      = os.path.join(_HERE, "FOWT Visualisation Simulation", "VAWT Turbine simulation_Data", "StreamingAssets")
HEIGHTMAP_OUT  = os.path.join(STREAMING, "heightmaps")
FLOWMAP_OUT    = os.path.join(STREAMING, "flowmaps")
STREAMLINE_OUT = os.path.join(STREAMING, "Streamline")
WINDDATA_OUT   = os.path.join(STREAMING, "winddata")


def has_data(folder):
    if not os.path.exists(folder):
        return False
    return any(f.endswith(".csv") for f in os.listdir(folder))


def ensure_dirs():
    for d in [WATER_INPUT, WIND_INPUT, TURBINE_INPUT, HEIGHTMAP_WORK,
              HEIGHTMAP_OUT, FLOWMAP_OUT, STREAMLINE_OUT, WINDDATA_OUT]:
        os.makedirs(d, exist_ok=True)


def run_pvpython(script, *args):
    cmd = [PVPYTHON, os.path.join(_HERE, script), *args]
    print(f"  Running: {' '.join(cmd)}")
    result = subprocess.run(cmd, check=True)
    return result


def copy_files(src_folder, dst_folder, extension=".csv"):
    for f in os.listdir(src_folder):
        if f.endswith(extension):
            src = os.path.join(src_folder, f)
            dst = os.path.join(dst_folder, f)
            import shutil
            shutil.copy2(src, dst)
            print(f"  Copied: {f}")


def main():
    print("=== FOWT Visualisation Pipeline ===\n")

    ensure_dirs()

    # ── Step 1: Extract from ParaView (skip if data already exists) ──────────
    if has_data(WATER_INPUT) and has_data(WIND_INPUT) and has_data(TURBINE_INPUT):
        print("[1/4] Simulation_Input has data — skipping ParaView extraction\n")
    else:
        print("[1/4] Extracting from OpenFOAM via ParaView...")
        run_pvpython("extract_water.py",        CASE_FILE, WATER_INPUT)
        run_pvpython("extract_streamlines.py",  CASE_FILE, WIND_INPUT)
        run_pvpython("Extract_fowt.py",         CASE_FILE, os.path.join(TURBINE_INPUT, "wind_data.csv"))
        print()

    # ── Step 2: Process water CSVs → heightmap PNGs ──────────────────────────
    print("[2/4] Processing water surface CSVs into heightmaps...")
    from process_water import generate_heightmaps
    generate_heightmaps(WATER_INPUT, HEIGHTMAP_WORK)
    print()

    # ── Step 3: Optical flow — heightmaps → flow maps ────────────────────────
    print("[3/4] Computing optical flow maps...")
    cmd = [sys.executable, os.path.join(_HERE, "optical_flow.py"), HEIGHTMAP_WORK, FLOWMAP_OUT]
    print(f"  Running: {' '.join(cmd)}")
    subprocess.run(cmd, check=True)
    print()

    # ── Step 4: Copy assets into the built Unity app ─────────────────────────
    print("[4/4] Copying assets into VAWT Turbine simulation...")

    import shutil

    # Copy heightmaps
    for f in os.listdir(HEIGHTMAP_WORK):
        if f.endswith(".png"):
            shutil.copy2(os.path.join(HEIGHTMAP_WORK, f), os.path.join(HEIGHTMAP_OUT, f))
    print(f"  Heightmaps → {HEIGHTMAP_OUT}")

    # Copy streamline CSVs
    copy_files(WIND_INPUT, STREAMLINE_OUT, ".csv")
    print(f"  Streamlines → {STREAMLINE_OUT}")

    # Copy wind_data.csv
    wind_src = os.path.join(TURBINE_INPUT, "wind_data.csv")
    if os.path.exists(wind_src):
        shutil.copy2(wind_src, os.path.join(WINDDATA_OUT, "wind_data.csv"))
        print(f"  Wind data → {WINDDATA_OUT}")

    print("\n=== Done — launch VAWT Turbine simulation.exe ===")


if __name__ == "__main__":
    main()
