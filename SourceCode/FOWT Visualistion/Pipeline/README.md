# FOWT Visualisation Pipeline

Processes raw OpenFOAM simulation data and loads it directly into the built Unity app.

---

## Requirements

- Python 3.x with: `numpy`, `Pillow`, `scipy`, `opencv-python`
- ParaView with `pvpython` available on your system PATH

Install Python dependencies:
```
pip install numpy Pillow scipy opencv-python
```

---

## How to run

**1. Open `run_pipeline.py` and set your case path at the top:**
```python
CASE_FILE = "C:/path/to/your/case.foam"
```

**2. Run it:**
```
python run_pipeline.py
```

**3. Launch `FOWT Visualisation Simulation/VAWT Turbine simulation.exe`**

That's it. The pipeline writes all processed data directly into the app — no manual file copying needed.

---

## What it does

The pipeline runs in four steps:

**Step 1 — Extract from OpenFOAM** *(skipped automatically if Simulation_Input already has data)*
- `extract_water.py` — pulls the water surface point cloud at each timestep
- `extract_streamlines.py` — traces wind streamlines through the velocity field, recording actual wind speed per point
- `Extract_fowt.py` — probes wind speed at the turbine hub across all timesteps

**Step 2 — Process water**
- `process_water.py` — converts the water surface point clouds into 256×256 heightmap PNGs

**Step 3 — Optical flow**
- `optical_flow.py` — computes per-pixel motion vectors between consecutive heightmaps for smooth water animation

**Step 4 — Copy into the app**
- Moves all processed data into `FOWT Visualisation Simulation/VAWT Turbine simulation_Data/StreamingAssets/`

---

## Folder structure

```
Pipeline/
├── run_pipeline.py          ← run this
├── extract_water.py         ← ParaView: water surface extraction
├── extract_streamlines.py   ← ParaView: wind streamlines with velocity
├── Extract_fowt.py          ← ParaView: turbine hub wind speed
├── process_water.py         ← converts water CSVs to heightmap PNGs
├── optical_flow.py          ← computes flow maps from heightmaps
├── Simulation_Input/        ← intermediate data (auto-populated)
│   ├── water/
│   ├── wind/
│   └── turbine/
└── FOWT Visualisation Simulation/   ← the built Unity app
```

---

## Re-running with new simulation data

Delete the contents of `Simulation_Input/water/`, `wind/`, and `turbine/`, then run `run_pipeline.py` again. If those folders have data in them the extraction step is skipped, so clearing them forces a fresh extraction from ParaView.
