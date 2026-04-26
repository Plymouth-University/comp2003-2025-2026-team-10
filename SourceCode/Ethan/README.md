# Ethan — CFD Visualisation (Water Surface + Pipeline)

## Final deliverable

Everything needed for the client is in [`final version/`](final%20version/). See its README for full details.

## Two client options

**Option A — Pre-built app** (`final version/CFD_Visualisation_App/`)
A standalone Unity `.exe` with simulation data already included. Double-click to run. See `HOW_TO_BUILD.md` for how to package it from Unity.

**Option B — Full pipeline** (`final version/FOWT-Vis-Pipeline/`)
Complete Python pipeline + Unity scripts. Allows loading new OpenFOAM simulation datasets. Two commands:
```
pvpython extract_all.py     # extract from OpenFOAM (ParaView)
python run_pipeline.py      # process and push to Unity
```
Then press Play in Unity.

## Other folders

| Folder | Contents |
|---|---|
| `outdated/` | Early Gerstner wave experiments — superseded by heightmap approach |
| `pipeline/` | Intermediate pipeline development — superseded by `final version/` |
| `Python Scripts/` | Standalone utility scripts used during development |
| `Unity Scripts/` | Earlier Unity water scripts — final versions are in `final version/` |
