README covers everything needed to get the wind and wave simulation
running in Unity.

---

## Python setup (run before Unity)

Requirements:

```
pip install pandas numpy
```

### Option A — Use CSVs directly in Unity (simpler)

Skip the Python steps entirely and drag CSVs straight into Unity as TextAssets.
Only recommended if your CSVs are already small (under ~5MB each).

### Option B — Pre-bake to binary (recommended for large files)

**Step 1 — Inspect your CSV columns:**

```bash
python csv_to_simdata.py --input wind.csv --inspect
python csv_to_simdata.py --input waves.csv --inspect
```

Check the printed output matches: `time, vx, vy, vz, id, x, y, z`

**Step 2 — Convert to binary:**

```bash
python csv_to_simdata.py --input wind.csv  --output wind.simdata  --voxel 0.5
python csv_to_simdata.py --input waves.csv --output waves.simdata --voxel 0.3
```

The `--voxel` value controls spatial reduction. Larger = smaller file, less detail.
Start at `0.5` for wind and `0.3` for waves.

**Step 3 — Place `.simdata` files in Unity:**

```
Assets/StreamingAssets/wind.simdata
Assets/StreamingAssets/waves.simdata
```

Create the `StreamingAssets` folder if it doesn't exist.

---

## Unity setup — step by step

### 1. Add scripts to your project

Copy all four `.cs` files into `Assets/Scripts/`. Unity compiles them automatically.

---

### 2. Create the SimulationManager

This is the clock that drives both wind and waves from the same timestep.

1. Hierarchy → right-click → **Create Empty** → rename it `SimulationManager`
2. Inspector → **Add Component** → `SimulationClock`
3. Set these values:

| Field        | Value                                 |
| ------------ | ------------------------------------- |
| Frame Count  | Number of timestep CSV files you have |
| Playback FPS | `10` (adjust to taste)                |
| Loop         | ✅ ticked                             |
| Is Playing   | ✅ ticked                             |

---

### 3. Set up the Wind

1. Hierarchy → **Create Empty** → rename `WindSystem`
2. **Add Component** → `StreamlineSystem`
3. Fill in the Inspector:

| Field                    | What to assign                                      |
| ------------------------ | --------------------------------------------------- |
| Csv File                 | Drag your wind CSV TextAsset here                   |
| Particle Prefab          | Wind particle prefab (e.g. small white/grey sphere) |
| Particle Parent          | Drag the `WindSystem` GameObject here               |
| Speed                    | `1`                                                 |
| Particles Per Streamline | `1`                                                 |
| Clock                    | Drag `SimulationManager` here                       |

> **If using binary (.simdata):** also Add Component → `SimDataLoader`,
> set File Name to `wind.simdata`, and leave Csv File empty.

---

### 4. Set up the Waves

Waves use the same `StreamlineSystem` script — just a second instance with a
different CSV and a different particle prefab to keep them visually distinct.

1. Hierarchy → **Create Empty** → rename `WaveSystem`
2. **Add Component** → `StreamlineSystem`
3. Fill in the Inspector:

| Field                    | What to assign                                |
| ------------------------ | --------------------------------------------- |
| Csv File                 | Drag your waves CSV TextAsset here            |
| Particle Prefab          | Wave particle prefab (e.g. small blue sphere) |
| Particle Parent          | Drag the `WaveSystem` GameObject here         |
| Speed                    | `1`                                           |
| Particles Per Streamline | `1`                                           |
| Clock                    | Drag `SimulationManager` here                 |

> **If using binary (.simdata):** also Add Component → `SimDataLoader`,
> set File Name to `waves.simdata`, and leave Csv File empty.

---

### 5. Check the Console on first Play

Press **Play** and check the Unity Console. You should see:

```
Loaded streamlines: 42
Combined streamline paths: 21
Loaded streamlines: 38
Combined streamline paths: 19
```

Numbers will vary. If you see errors, check the troubleshooting table below.

---

## Scene hierarchy

```
Scene
├── SimulationManager       ← SimulationClock.cs
├── WindSystem              ← StreamlineSystem.cs  (+ SimDataLoader.cs if binary)
└── WaveSystem              ← StreamlineSystem.cs  (+ SimDataLoader.cs if binary)
```

---

## Troubleshooting

| Error                                 | Fix                                                                       |
| ------------------------------------- | ------------------------------------------------------------------------- |
| `NullReferenceException` on csvFile   | CSV TextAsset not assigned in Inspector                                   |
| `File not found: wind.simdata`        | File is not in `Assets/StreamingAssets/`                                  |
| Particles all spawn at origin (0,0,0) | CSV column order is wrong — run `--inspect` and check                     |
| Wind and waves out of sync            | Both GameObjects need the same `SimulationManager` in the Clock field     |
| Particles invisible                   | Check particle prefab has a Renderer and is not scaled to zero            |
| Very slow startup                     | Switch to binary loading using `csv_to_simdata.py` and `SimDataLoader.cs` |

---

## Important note — CSV column order

`csv_to_simdata.py` and `StreamlineSystem.cs` both expect this column order:

```
time, vx, vy, vz, id, x, y, z
```

If your CSVs use different names or order, run `--inspect` first and update
the `COL_X`, `COL_Y`, `COL_VX` etc. constants at the top of `csv_to_simdata.py`.

---

## Coming later

A unified per-frame streaming pipeline (one merged wind+wave `.simdata` file
per timestep, loaded on demand) will be added once the CSV folder structure
is confirmed. Everything above will still work — it builds on top of this.
