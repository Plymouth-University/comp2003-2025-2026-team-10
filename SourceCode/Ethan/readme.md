# Procedural Wave Generation Pipeline

**CFD → ParaView → Python → Unity**

## Overview

This workflow demonstrates how CFD wave data from an OpenFOAM simulation can be converted into a lightweight, procedurally generated ocean surface in Unity.

The primary goal is to **avoid storing large CFD datasets** while preserving realistic wave behaviour by extracting high-level wave characteristics and regenerating them mathematically at runtime.

The pipeline consists of three stages:

1. Extract free-surface motion from CFD data using ParaView
2. Process the extracted data in Python to derive wave parameters
3. Use those parameters in Unity to generate waves procedurally (Gerstner waves)

Two Python scripts are provided:

* One for **testing and Unity integration** using synthetic wave parameters
* One for **deriving wave parameters from ParaView-exported CFD data**

---

## Stage 1: Extracting Free-Surface Motion in ParaView

### Purpose

OpenFOAM represents two-phase flow using the field `alpha.water`, which stores the water volume fraction.
The water–air interface (free surface) is commonly approximated by the isosurface:

```
alpha.water = 0.5
```

ParaView is used to extract this interface and sample its vertical motion over time at a fixed horizontal location.

---

### Step-by-Step (ParaView)

#### 1. Load the OpenFOAM case

* File → Open → select the OpenFOAM case file (typically `case.foam`)
* Click **Apply**

#### 2. Enable the `alpha.water` field

* Select the OpenFOAM reader in the Pipeline Browser
* In Properties, enable `alpha.water`
* Click **Apply**

#### 3. Extract the free surface (interface)

* With the reader selected: **Filters → Contour**
* Set:

  * **Contour By:** `alpha.water`
  * **Isosurfaces:** `0.5`
* Click **Apply**

This produces a surface mesh representing the water–air interface for each timestep.

#### 4. Sample surface height using a vertical line

* Select the **Contour** output
* Filters → Data Analysis → **Plot Over Line**
* Define a vertical sampling line at a fixed horizontal location:

  * `Point1 = (x, y_low, z)`
  * `Point2 = (x, y_high, z)`
* Choose `y_low` below the minimum water level and `y_high` above the maximum.
* Click **Apply**

This samples the interface along a vertical line at the chosen `(x, z)` location.

#### 5. Convert line samples into a time series

* Select the **PlotOverLine** output
* Filters → Temporal → **Plot Data Over Time**
* Click **Apply**

This produces line-sample values across all timesteps.

#### 6. Save the data to CSV

* Select the **Plot Data Over Time** output
* File → Save Data
* Choose **CSV (*.csv)**
* Ensure **Write Time Steps** is enabled
* Save as:

```
paraview_line_over_time.csv
```

---

### Output of the ParaView Stage

The resulting CSV file contains:

* Multiple rows per timestep (one per sampled point along the vertical line)
* Time values
* Point coordinates (including Y values)

This file represents the free-surface elevation over time and serves as the input to the Python processing stage.

---

## Stage 2: Python Processing Scripts

### Script 1: `gerstner_wave_generation_testing.py`

#### Purpose

* Generate plausible Gerstner wave parameters without using CFD data
* Test the Unity pipeline (JSON loading and procedural wave rendering)

#### What it does

* Generates multiple wave components with varying:

  * Direction
  * Amplitude
  * Wavelength
  * Phase
  * Speed
  * Steepness
* Exports the parameters to a Unity-friendly JSON file

#### Why it exists

* Allows Unity development to proceed independently of CFD processing
* Provides a clear reference for Gerstner wave parameter structure

#### Output

```
waves_testing.json
```

This file is loaded directly by Unity and used to generate waves at runtime.

---

### Script 2: `gerstner_waves_from_paraview_csv.py`

#### Purpose

* Derive Gerstner wave parameters from CFD-derived free-surface motion
* Demonstrate that wave parameters can be extracted from simulation data

#### Input

```
paraview_line_over_time.csv
```

#### What it does

1. Groups CSV rows by timestep
2. Extracts the free-surface height η(t) for each timestep

   * Uses the median Y coordinate of contour intersection points
3. Computes wave characteristics:

   * Amplitude from peak-to-peak height
   * Dominant frequency via FFT
   * Angular frequency: `ω = 2πf`
4. Constructs Gerstner wave parameters
5. Exports a Unity-compatible JSON file

#### Important clarification

* OpenFOAM fields named `k` and `omega` are **turbulence-model variables**
* In this pipeline:

  * `k = 2π / L` → wave number
  * `omega = 2πf` → wave angular frequency

#### Output

```
waves_from_paraview.json
```

This JSON file can be loaded directly by Unity to recreate CFD-derived wave behaviour procedurally.

---

## Stage 3: Unity Usage (High-Level)

Within Unity, the procedural wave system is driven entirely by a lightweight JSON configuration generated during the Python preprocessing stage.

At application startup:
	•	The JSON file containing the wave parameters is loaded once.
	•	Parameters such as amplitude, wavelength, direction, phase, and speed are parsed and stored in arrays or passed to shader constants.
	•	The GerstnerWaterFromJson.cs script is responsible for reading the JSON file, managing the wave parameter data, and updating the water surface.

During runtime:
	•	Each frame, GerstnerWaterFromJson.cs evaluates the Gerstner wave equations using the stored parameters.
	•	Vertex displacement is computed analytically to animate the water surface in real time.
	•	No CFD data or large datasets are loaded once the application is running.

This approach ensures that the visual behaviour of the waves is reconstructed procedurally, with minimal memory usage and negligible runtime overhead.

---

## Summary
  * ParaView extracts free-surface motion from the alpha.water field in the CFD dataset.
  * Python processes this motion and converts it into compact Gerstner wave parameters.
  * These parameters are exported as a lightweight JSON file.
  * Unity loads the JSON file at startup and uses GerstnerWaterFromJson.cs to drive procedural wave generation.
  * Waves are regenerated analytically in real time without loading CFD data.
  * Storage requirements and hardware demands are reduced by orders of magnitude.