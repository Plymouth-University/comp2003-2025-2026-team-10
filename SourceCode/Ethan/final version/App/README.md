# CFD Visualisation — App

This is the pre-populated version of the visualisation. The Unity project comes with wind streamline data already in place and is designed to be opened and played immediately once the remaining simulation data is added.

The visualisation has three components that animate automatically on Play:

| Component | Data source |
|---|---|
| Water surface | Heightmap PNGs in `Unity Project/Assets/StreamingAssets/HeightMaps/` |
| Wind streamlines | CSV files in `Unity Project/Assets/Resources/Streamline/` |
| Turbine motion | `Unity Project/Assets/StreamingAssets/Turbine/turbine.csv` |

Wind CSV data is already included. Before playing, drop the heightmap PNGs and `turbine.csv` into the corresponding folders above. The number of heightmap frames is detected automatically — drop in as many as you have.

The Unity project reads all data at runtime so no recompilation is needed. Open `Unity Project` in Unity Hub, wait for import, then press Play.

Use this version when you want to view the visualisation without running the Python pipeline. Use the Pipeline version if you need to re-process simulation data or modify the scripts.

## Controls

| Control | Action |
|---|---|
| Hold right mouse + W/A/S/D | Move through the scene |
| Hold right mouse + move mouse | Look around |
| Hold right mouse + E / Q | Move camera up / down |
| Scroll wheel | Zoom |

## Requirements

- Unity 2022 LTS or later
- Windows 10 64-bit or later
- 8 GB RAM minimum
