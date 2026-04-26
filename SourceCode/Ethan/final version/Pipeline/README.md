# CFD Visualisation — Pipeline

This is the full, editable version of the visualisation. It contains the complete Python processing pipeline and an empty Unity project that is populated by running it.

The pipeline is split into three independent modules — one per data type — so any part can be modified or re-run without touching the others:

| Script | What it does |
|---|---|
| `extract_all.py` | Opens the OpenFOAM case in ParaView and extracts water, wind, and turbine data as CSVs |
| `process_water.py` | Converts water surface point clouds into 256×256 heightmap PNGs |
| `process_wind.py` | Renames and copies wind streamline CSVs into the Unity project |
| `process_turbine.py` | Validates and copies the turbine motion CSV into the Unity project |
| `run_pipeline.py` | Orchestrates all three processing steps in sequence |

Raw simulation data goes into `Simulation_Input/` and processed output is written directly into `Unity Project/Assets/` — so opening the Unity project and pressing Play after running the pipeline is all that is needed.

The Unity project loads all data at runtime from `StreamingAssets/` and `Resources/`, meaning no recompilation is needed when simulation data changes. The number of heightmap frames and wind timestep files is detected automatically.

Use this version to re-run the pipeline with new simulation data, adjust processing parameters, or modify the Unity scripts.
