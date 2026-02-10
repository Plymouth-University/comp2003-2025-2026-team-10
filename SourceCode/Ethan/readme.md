# CFD Water Surface Reconstruction Pipeline

**OpenFOAM → ParaView → Python → Unity**

## Overview

This project demonstrates a complete pipeline for converting CFD free-surface wave data into a real-time animated water surface in Unity.

Originally, the workflow extracted wave characteristics from CFD data and regenerated them using procedural Gerstner waves in Unity. While this approach was lightweight and mathematically elegant, it produced overly smooth and idealised waves and did not preserve turbulence or wake effects visible in the CFD simulation.

To achieve higher visual fidelity, the pipeline was upgraded to use heightmaps with temporal interpolation. This allows Unity to reconstruct the exact surface geometry from the CFD simulation while remaining efficient enough for real-time rendering.

The current pipeline consists of:

1. Extract free-surface geometry from CFD using ParaView
2. Convert sampled surface data into heightmap images using Python
3. Animate a high-resolution mesh in Unity using interpolated heightmaps

---

## Stage 1: Free-Surface Extraction in ParaView

### Purpose

OpenFOAM stores the water–air interface using the scalar field:

```
alpha.water
```

The free surface is extracted as the isosurface:

```
alpha.water = 0.5
```

This produces a surface mesh representing the water interface at each timestep.

### Steps (ParaView)

1. **Load the OpenFOAM case**
   - File → Open → `case.foam`
   - Apply

2. **Enable** `alpha.water`

3. **Apply Contour filter**
   - Contour By: `alpha.water`
   - Isosurface: `0.5`

4. **Export surface sampling data**
   - Use Plot Over Line or surface sampling
   - Save each timestep to CSV

These CSV files contain:

- Time
- Points:0 (X)
- Points:1 (Y)
- Points:2 (Z / height)
- WaterHeight

---

## Stage 2: Python Heightmap Generation

### Purpose

The Python script converts CFD surface CSV data into grayscale heightmaps.

Each heightmap represents one timestep of the water surface.

### What the script does

For each CSV file:

- Reads X, Y, Z surface coordinates
- Builds a regular 2D grid
- Normalizes height values
- Exports a PNG heightmap

**Output:**

```
heightmap_0.png
heightmap_1.png
...
heightmap_10.png
```

**Grayscale meaning:**

- **White** = highest water elevation
- **Black** = lowest water elevation

These heightmaps preserve:

- Wave shape
- Wake turbulence
- Spatial variation
- Real CFD behaviour

---

## Stage 3: Unity Reconstruction (Heightmap Animation)

### Mesh Generation

A custom Plane Generator script creates a high-resolution mesh:

- Adjustable plane size
- Adjustable resolution (vertex density)

This ensures smooth deformation and avoids the "folded paper" effect seen with low-resolution planes.

### Heightmap Animation

A Unity script:

- Loads all heightmaps from a folder
- Applies them to the mesh sequentially
- Interpolates (LERP) between frames for smooth motion
- Loops animation continuously

This recreates the CFD surface in real time.

### Key Features

- Heightmap-driven deformation (not procedural noise)
- Frame interpolation for smooth animation
- High-resolution mesh for realistic curvature
- Real-time playback in Unity

---

## Why Heightmaps Replaced Gerstner Waves

### Original method:
- FFT → wave parameters → Gerstner waves
- Lightweight and compact
- Lost turbulence and wake features
- Looked artificial compared to CFD

### Final method:
- Direct geometry reconstruction
- Preserves physical simulation features
- Matches ParaView visually
- Still efficient for real-time use

---

## Summary

- ParaView extracts the free surface from CFD using `alpha.water = 0.5`
- Python converts sampled surface data into heightmaps
- Unity animates a high-resolution mesh using interpolated heightmaps
- The result is a real-time reconstruction of CFD water motion
- This method preserves realism while remaining computationally efficient