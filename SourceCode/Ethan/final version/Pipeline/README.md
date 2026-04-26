# CFD Visualisation — Pipeline

This is the complete, editable pipeline used to produce the visualisation. It contains the full Python processing pipeline and a configured Unity project with all simulation data already in place.

The pipeline is split into three independent modules — one per data type — so any part can be modified or re-run independently when new simulation data is available:

| Script | What it does |
|---|---|
| `extract_all.py` | Opens the OpenFOAM case in ParaView and extracts water, wind, and turbine data as CSVs |
| `process_water.py` | Converts water surface point clouds into 256×256 heightmap PNGs |
| `process_wind.py` | Renames and copies wind streamline CSVs into the Unity project |
| `process_turbine.py` | Validates and copies the turbine motion CSV into the Unity project |
| `run_pipeline.py` | Orchestrates all three processing steps in sequence |

Processed data lives in `Simulation_Input/` and is written directly into `Unity Project/Assets/` by `run_pipeline.py`. The Unity project reads all data at runtime so no recompilation is needed when simulation data changes — the number of heightmap frames and wind timestep files is detected automatically.

Use this version to re-run the pipeline with new simulation data, adjust processing parameters, or modify the Unity scripts. The App folder contains the same Unity project pre-packaged for viewing without any pipeline setup.
