# Adding Pressure & Vorticity to the App

## Files to add

| File | Where |
|---|---|
| `PressureMapVisualiser.cs` | `Assets/Scripts/` |
| `VorticityVisualiser.cs` | `Assets/Scripts/` |
| `PressureOverlay.shader` | `Assets/Shaders/` (create if missing) |
| `VorticityOverlay.shader` | `Assets/Shaders/` (create if missing) |

## File to replace

| File | Where |
|---|---|
| `WaterSurface.cs` | `Assets/water/scripts/` |

## Unity setup

1. Open the scene and click the **WaterSurface** GameObject in the Hierarchy
2. Click **Add Component** → search `PressureMapVisualiser` → add it
3. Click **Add Component** → search `VorticityVisualiser` → add it
4. Set `Scene Position` on both components to match the WaterSurface Transform position
5. Leave all folder names as defaults (`PressureMaps`, `VorticityMaps`)

## Data

Create these folders inside `Assets/StreamingAssets/` and add the relevant PNGs:

```
StreamingAssets/PressureMaps/pressuremap_0.png, pressuremap_1.png ...
StreamingAssets/VorticityMaps/vorticitymap_0.png, vorticitymap_1.png ...
```

Test PNGs are provided in the repo to confirm the setup works before real pipeline data is available.

## Press Play

Both overlays load and animate automatically at runtime — no further setup needed.
