# CFD Visualisation — Final Version

**COMP2003 Team 10 | University of Plymouth | 2025–2026**

This folder contains the complete, consolidated pipeline that converts OpenFOAM CFD simulation data into a real-time interactive 3D visualisation in Unity. It replaces all individual team member pipelines with a single two-command workflow.

---

## What this visualisation does

The simulation produces raw OpenFOAM data. This pipeline extracts three data streams from it and delivers them to Unity:

| Component | Source data | Output |
|---|---|---|
| Water surface | `alpha.water` isosurface (CFD) | 256×256 heightmap PNGs, lerped each frame |
| Wind streamlines | Velocity field `U` streamlines (CFD) | CSV files loaded into Unity particle system |
| Turbine motion | Hub wind speed + platform displacement (CFD) | CSV with surge, heave, pitch per timestep |

---

## Folder structure

```
final version/
├── App/                        ← Complete Unity project with all data included
│   ├── README.md
│   └── Unity Project/          ← Open this in Unity Hub and press Play
│       └── Assets/
│           ├── Scripts/
│           ├── Resources/
│           │   └── Streamline/ ← Wind CSVs (included)
│           └── StreamingAssets/
│               ├── HeightMaps/ ← Heightmap PNGs (included)
│               └── Turbine/    ← turbine.csv (included)
│
└── Pipeline/                   ← Full editable pipeline + Unity project
    ├── README.md
    ├── Unity Project/          ← Open this in Unity Hub
    │   └── Assets/
    │       ├── Scripts/        All 5 C# scripts
    │       ├── Resources/
    │       │   └── Streamline/ ← Populated by run_pipeline.py
    │       └── StreamingAssets/
    │           ├── HeightMaps/ ← Populated by run_pipeline.py
    │           └── Turbine/    ← Populated by run_pipeline.py
    ├── Simulation_Input/
    │   ├── water/              ← Drop wave_N.csv files here
    │   ├── wind/               ← Drop N.csv wind files here
    │   └── turbine/            ← Drop turbine.csv here
    ├── run_pipeline.py         ← Processes CSVs and writes into Unity Project
    ├── extract_all.py          ← Run with pvpython to extract from OpenFOAM
    ├── process_water.py
    ├── process_wind.py
    └── process_turbine.py
```

---

## Using the App

Open `App/Unity Project` in Unity Hub and press Play. All data is included — no setup required.

## Using the Pipeline

**Step 1 — Extract from OpenFOAM** *(skip if you already have CSVs)*

Edit `CASE_FILE` at the top of `extract_all.py` to point to your `.foam` file, then run:
```
pvpython extract_all.py
```

**Step 2 — Process and push to Unity**
```
python run_pipeline.py
```
No path configuration needed — the script locates the Unity project automatically.

**Step 3 — Play**

Open `Pipeline/Unity Project` in Unity Hub and press Play.

---

## Unity scene setup

All scripts are already inside each Unity project. Attach them to GameObjects as follows:

| Script | Attach to | Set in Inspector |
|---|---|---|
| `HeightMapWaterPlane.cs` | Empty GameObject (water plane) | `planeSize` to match domain |
| `PlaneGenerator.cs` | Same GameObject as above | — |
| `CombinedStreamlineSystem.cs` | Empty GameObject | `particlePrefab`, `particleParent` |
| `FOWTController.cs` | FOWT root GameObject | `platformTransform`, `bladeTransform` |
| `BobbingController.cs` | Same as FOWTController | `bobFrequency`, `bobAmplitude` |

Scene positions are hardcoded in each script — no manual alignment needed:
- Water plane → `(7.5, 0, 0)` — centre of the 0–15 m simulation domain
- FOWT → `(7.5, 2.135, 0)` — hub height from CFD

---

## How each script works

### `extract_all.py`
Runs inside ParaView's bundled Python interpreter (`pvpython`). Opens the OpenFOAM case once and runs three extraction pipelines per timestep:
- **Water**: Contour filter at `alpha.water = 0.5` → CSV of surface point coordinates
- **Wind**: StreamTracer on velocity field `U` → CSV with `RegionId, x, y, z` per point
- **Turbine**: Hub probe for wind speed + platform point displacement → scalars per timestep

All output goes to `Simulation_Input/`.

### `run_pipeline.py`
Orchestrates the three processing modules and writes output directly into the Unity project. Paths are resolved relative to the script — no configuration needed:
- `process_water.py` → bins point cloud to 256×256 grid, normalises, Gaussian blur, 16-bit PNG
- `process_wind.py` → clears old CSVs from `Resources/Streamline/`, copies renamed wind CSVs
- `process_turbine.py` → validates column structure, copies to `StreamingAssets/Turbine/`

### `HeightMapWaterPlane.cs`
Generates a procedural plane mesh at runtime and deforms it each frame by sampling two heightmap PNGs and lerping between them. Automatically detects how many heightmaps are present — no frame count configuration needed. Starts from frame 1 to avoid CFD startup transient artifacts. Applies a `bobbingOffset` written by `BobbingController.cs` each frame.

### `CombinedStreamlineSystem.cs`
Loads all wind CSVs from `Resources/Streamline/` at startup. Reconstructs full streamlines by combining backward and forward integration halves. Uses arc-length parametrisation and Catmull-Rom spline interpolation for smooth constant-speed particle flow. Cycles through timestep files every `fileSwitchInterval` seconds.

### `FOWTController.cs`
Reads `turbine.csv` from `StreamingAssets/Turbine/` at startup. Lerps platform position and pitch between snapshots. Rotates blades at a speed proportional to wind speed × tip-speed ratio.

---

## Dependencies

| Tool | Version | Purpose |
|---|---|---|
| Python | 3.10+ | Processing pipeline |
| numpy | any | Array maths in `process_water.py` |
| Pillow | any | PNG export |
| scipy | any | Gaussian blur |
| ParaView | 5.11+ | CFD data extraction (`extract_all.py` only) |
| Unity | 2022 LTS+ | Visualisation |

---
