"""


preprocessing pipeline you run once on your raw
OpenFOAM/ParaView exports before bringing
anything into Unity.

Pipeline per timestep:

  1. Load CSV
  
  2. Keep only the required columns (x, y, z, velocity, pressure)
  
  3. Spatially downsample using voxel-grid averaging (no temporal interpolation)
  
  4. Write reduced CSV to output folder

Usage:
  python reduce_paraview_csv.py --input ./simulation_data --output ./unity_data --voxel 0.5

Requirements:
  pip install pandas numpy
"""

import os
import argparse
import numpy as np
import pandas as pd


COORD_COLS   = ["Points:0", "Points:1", "Points:2"]   # x, y, z
VELOCITY_COLS = ["U:0", "U:1", "U:2"]                 # Ux, Uy, Uz
PRESSURE_COL  = ["p"]                                  # pressure

# Output column names written to the Unity CSV
OUTPUT_COLS = ["x", "y", "z", "Ux", "Uy", "Uz", "p"]

# Alternative common column names (ParaView versions vary)
ALT_COORD_COLS    = [["x", "y", "z"], ["Points_0", "Points_1", "Points_2"]]
ALT_VELOCITY_COLS = [["U_0", "U_1", "U_2"], ["velocity:0", "velocity:1", "velocity:2"]]
ALT_PRESSURE_COLS = [["p"], ["pressure"]]


def detect_columns(df_cols):
    """
    Auto-detect coordinate, velocity, and pressure column names
    from the actual headers in the CSV.
    """
    def find(candidates_list, df_cols):
        for candidates in candidates_list:
            if all(c in df_cols for c in candidates):
                return candidates
        return None

    coords   = find([COORD_COLS]    + ALT_COORD_COLS,    df_cols)
    velocity = find([VELOCITY_COLS] + ALT_VELOCITY_COLS, df_cols)
    pressure = find([PRESSURE_COL]  + ALT_PRESSURE_COLS, df_cols)

    if coords is None:
        raise ValueError(
            f"Could not find coordinate columns. Headers found: {list(df_cols)}\n"
            "Edit COORD_COLS at the top of this script to match your CSV headers."
        )
    if velocity is None:
        print("  [warn] Velocity columns not found — they will be skipped.")
    if pressure is None:
        print("  [warn] Pressure column not found — it will be skipped.")

    return coords, velocity, pressure


def voxel_downsample(df, coords, velocity, pressure, voxel_size):
    """
    Reduce point count by grouping points into a 3-D voxel grid and
    averaging all values within each voxel cell.

    Parameters
    ----------
    voxel_size : float
        Side length of each voxel cube (in the same units as x/y/z).
        Larger = fewer output points, more compression.
    """
    x, y, z = coords

    # Assign each point to a voxel cell using integer division
    df = df.copy()
    df["_vx"] = np.floor(df[x] / voxel_size).astype(int)
    df["_vy"] = np.floor(df[y] / voxel_size).astype(int)
    df["_vz"] = np.floor(df[z] / voxel_size).astype(int)

    group_cols = ["_vx", "_vy", "_vz"]
    agg_cols   = {x: "mean", y: "mean", z: "mean"}

    if velocity:
        for col in velocity:
            agg_cols[col] = "mean"
    if pressure:
        for col in pressure:
            agg_cols[col] = "mean"

    reduced = df.groupby(group_cols, sort=False).agg(agg_cols).reset_index(drop=True)

    # Rename to clean Unity-friendly headers
    rename_map = {x: "x", y: "y", z: "z"}
    if velocity:
        vel_names = ["Ux", "Uy", "Uz"]
        rename_map.update(dict(zip(velocity, vel_names)))
    if pressure:
        rename_map[pressure[0]] = "p"

    reduced.rename(columns=rename_map, inplace=True)
    return reduced


def process_csv(input_path, output_path, voxel_size):
    """Load, filter, downsample and save a single CSV file."""
    df = pd.read_csv(input_path)

    coords, velocity, pressure = detect_columns(df.columns)

    # Keep only the columns we need
    keep = list(coords)
    if velocity:
        keep += velocity
    if pressure:
        keep += pressure
    df = df[keep]

    original_count = len(df)

    
    reduced = voxel_downsample(df, coords, velocity, pressure, voxel_size)

    reduced_count = len(reduced)
    ratio = (1 - reduced_count / original_count) * 100 if original_count > 0 else 0

    reduced.to_csv(output_path, index=False, float_format="%.6f")

    print(f"    {os.path.basename(input_path)}: "
          f"{original_count:,} pts -> {reduced_count:,} pts "
          f"({ratio:.1f}% reduction)")


def process_all(input_dir, output_dir, voxel_size):
    """
    Walk the input directory tree.
    Expects structure:  input_dir / <timestep_folder> / <file>.csv
    """
    os.makedirs(output_dir, exist_ok=True)

    timestep_folders = sorted([
        f for f in os.scandir(input_dir)
        if f.is_dir()
    ], key=lambda e: e.name)

    if not timestep_folders:
    
        timestep_folders = [None]

    total_files = 0

    for entry in timestep_folders:
        if entry is None:
            folder_path = input_dir
            out_folder  = output_dir
            folder_name = "."
        else:
            folder_path = entry.path
            out_folder  = os.path.join(output_dir, entry.name)
            folder_name = entry.name

        csv_files = [f for f in os.listdir(folder_path) if f.lower().endswith(".csv")]

        if not csv_files:
            continue

        os.makedirs(out_folder, exist_ok=True)
        print(f"\n  Timestep folder: {folder_name}  ({len(csv_files)} file(s))")

        for csv_file in sorted(csv_files):
            in_path  = os.path.join(folder_path, csv_file)
            out_path = os.path.join(out_folder, csv_file)
            process_csv(in_path, out_path, voxel_size)
            total_files += 1

    print(f"\nDone. Processed {total_files} file(s). Output: {output_dir}")


def inspect_folder(input_dir):
    """
    Scan up to the first CSV found in input_dir (or any subfolder)
    and print its columns, shape, and a sample row so you know what
    to keep before running the full reduction.
    """
    print(f"\nInspecting: {input_dir}\n")
    found = False

    for root, dirs, files in os.walk(input_dir):
        dirs.sort()
        for fname in sorted(files):
            if fname.lower().endswith(".csv"):
                fpath = os.path.join(root, fname)
                df = pd.read_csv(fpath, nrows=5)

                print(f"  File   : {os.path.relpath(fpath, input_dir)}")
                print(f"  Rows   : {sum(1 for _ in open(fpath)) - 1:,}  (approx)")
                print(f"  Columns ({len(df.columns)}):")
                for i, col in enumerate(df.columns):
                    sample = df[col].iloc[0] if len(df) > 0 else "n/a"
                    print(f"    [{i:>2}]  {col:<35}  sample: {sample}")

                print(f"\n  Tip: copy the column names you want to keep into")
                print(f"       the KEEP_COLS list at the top of this script,")
                print(f"       then run with --mode reduce.\n")
                found = True
                break
        if found:
            break

    if not found:
        print("  No CSV files found. Check your --input path.")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------
def parse_args():
    parser = argparse.ArgumentParser(
        description="Reduce ParaView CSV exports for Unity (spatial voxel downsampling)."
    )
    parser.add_argument(
        "--input", "-i", required=True,
        help="Root folder containing per-timestep subfolders (or CSVs directly)."
    )
    parser.add_argument(
        "--output", "-o", default="./unity_output",
        help="Output folder. Mirrors the input folder structure. (used in reduce mode)"
    )
    parser.add_argument(
        "--voxel", "-v", type=float, default=0.5,
        help="Voxel size for spatial downsampling (same units as x/y/z). "
             "Default: 0.5. Increase to compress more aggressively."
    )
    parser.add_argument(
        "--mode", "-m", choices=["inspect", "reduce"], default="inspect",
        help="inspect: show columns in your CSVs (run this first). "
             "reduce: run the full reduction pipeline. Default: inspect"
    )
    parser.add_argument(
        "--keep", "-k", nargs="+", default=None,
        help="Explicit list of column names to keep (overrides auto-detection). "
             "Example: --keep x y z Ux Uy Uz p"
    )
    return parser.parse_args()


if __name__ == "__main__":
    args = parse_args()

    print(f"\nParaView CSV Reducer")
    print(f"  Mode   : {args.mode}")
    print(f"  Input  : {args.input}")

    if args.mode == "inspect":
        inspect_folder(args.input)

    elif args.mode == "reduce":
        print(f"  Output : {args.output}")
        print(f"  Voxel  : {args.voxel}")
        if args.keep:
            print(f"  Keep   : {args.keep}\n")
        else:
            print(f"  Keep   : auto-detect\n")
        process_all(args.input, args.output, args.voxel)
