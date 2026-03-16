# Simulation Data Processing Pipeline

This folder contains a prototype data pipeline that prepares simulation
outputs for use inside the Unity visualisation environment.

The goal is to automate the conversion of simulation data into
runtime-ready assets.

Pipeline stages:

Simulation Data
    ↓
Python Processing
    ↓
Unity Assets
    ↓
Real-Time Environment

## Components

Water
CFD free surface data is converted into heightmaps which drive
the animated ocean surface in Unity.

Wind
Wind simulation outputs are converted into vector fields used by
particle systems or shaders.

Turbine
Turbine simulation data is converted into parameters controlling
turbine animation and behaviour.

## Execution

Run:

python run_pipeline.py

The script reads simulation CSV files and generates the assets
required by the Unity project.