# Platform Pressure Subsystem — Setup Guide4

— jenson

## What this does

Visualises CFD pressure data directly on the turbine's floating platform (NewFloater) as a transparent blue-to-red colour overlay. High pressure areas appear red, low pressure areas appear blue. This shows the hydrodynamic wave loading on the platform hull — the structurally relevant part of the simulation for FOWT analysis.

This is more physically meaningful than the water surface pressure overlay since it shows pressure acting directly on the structure rather than on the surrounding fluid.

---

## Files to add

| File                            | Where                           |
| ------------------------------- | ------------------------------- |
| `PlatformPressureVisualiser.cs` | `Assets/Scripts/`               |
| `process_platform_pressure.py`  | alongside other process scripts |

## Files to replace

| File              | Where         |
| ----------------- | ------------- |
| `extract_all.py`  | pipeline root |
| `run_pipeline.py` | pipeline root |

Note: `PressureOverlay.shader` is reused from the existing pressure subsystem — no new shader needed.

---

## Unity setup

1. In the Hierarchy, expand the Turbine GameObject and click **NewFloater**
2. In the Inspector click **Add Component** → search `PlatformPressureVisualiser` → add it
3. Leave `Pressure Map Folder` as default (`PlatformPressureMaps`)
4. Set `Animation Speed` to match the simulation playback speed
5. Create `Assets/StreamingAssets/PlatformPressureMaps/` and add the pressure map PNGs named `platformpressure_0.png`, `platformpressure_1.png` etc.
6. Press Play — the overlay loads and animates automatically

Test PNGs are provided in the repo to confirm the setup works before real pipeline data is available.

---

## Pipeline

The platform pressure data flows through the pipeline the same way as water surface pressure:

```
FOWT_platform block (already in OpenFOAM case)
        ↓
pvpython extract_all.py → Simulation_Input/platform_pressure/platformpressure_N.csv
        ↓
process_platform_pressure.py → StreamingAssets/PlatformPressureMaps/platformpressure_N.png
        ↓
PlatformPressureVisualiser.cs → colour overlay on NewFloater mesh
```

No additional OpenFOAM data is needed — the `FOWT_platform` mesh region and `p` field are already loaded by the reader.

---

## Notes

- The overlay uses `PressureOverlay.shader` which must already be in `Assets/Shaders/` from the water surface pressure subsystem
- `NewFloater` is the correct target — it is the floating substructure hull that sits in the water and experiences wave pressure loading. The tower and blades are above the waterline and are not relevant for hydrodynamic pressure
- Unlike the water surface overlays, `PlatformPressureVisualiser` has no material race condition issues since `NewFloater` has no other scripts modifying its MeshRenderer at runtime
