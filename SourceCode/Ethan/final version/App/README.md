# CFD Visualisation — Pre-built App

## How to open

1. Open **Unity Hub**
2. Click **Open** → **Add project from disk**
3. Select the **Unity Project** folder inside this folder
4. Wait for Unity to import (2–5 minutes first time)
5. Press the **Play** button (triangle at the top of the screen)

The visualisation starts automatically. All simulation data is already included.

## Controls

| Control | Action |
|---|---|
| Hold right mouse + W/A/S/D | Move through the scene |
| Hold right mouse + move mouse | Look around |
| Hold right mouse + E | Move camera up |
| Hold right mouse + Q | Move camera down |
| Scroll wheel | Zoom |

## Requirements

- Unity 2022 LTS or later
- Windows 10 64-bit or later
- 8 GB RAM minimum

## Troubleshooting

**Water is flat** → Check that `Unity Project/Assets/StreamingAssets/HeightMaps/` contains the heightmap PNGs.

**No wind trails** → Check that `Unity Project/Assets/Resources/Streamline/` contains the wind CSV files.

**Turbine does not move** → Check that `Unity Project/Assets/StreamingAssets/Turbine/turbine.csv` exists.

**Low frame rate** → Select the water plane object in the scene → find `HeightMapWaterPlane` in the Inspector → reduce `planeResolution` to 60.
