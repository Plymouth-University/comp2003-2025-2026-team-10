"""
csv_to_simdata.py
=================
Converts ParaView/OpenFOAM streamline CSVs (wind or waves) into a compact
binary .simdata file for fast loading in Unity.

What it does:
  1. Reads the CSV (columns: time, vx, vy, vz, id, x, y, z)
  2. Optionally applies spatial voxel downsampling to reduce point count
  3. Writes a binary .simdata file that Unity's SimDataLoader.cs can read
     in a fraction of the time CSV parsing takes

Binary format (little-endian):
  [4 bytes]  magic number: 0x53494D44 ("SIMD")
  [4 bytes]  version: int32 = 1
  [4 bytes]  streamline count: int32
  per streamline:
    [4 bytes]  point count: int32
    per point:
      [4 bytes]  x:  float32
      [4 bytes]  y:  float32
      [4 bytes]  z:  float32
      [4 bytes]  vx: float32
      [4 bytes]  vy: float32
      [4 bytes]  vz: float32

Usage:
  # Inspect columns first
  python csv_to_simdata.py --input wind.csv --inspect

  # Convert with default settings
  python csv_to_simdata.py --input wind.csv --output wind.simdata

  # Convert with spatial downsampling (voxel size in simulation units)
  python csv_to_simdata.py --input wind.csv --output wind.simdata --voxel 0.5

  # Convert waves CSV (same format)
  python csv_to_simdata.py --input waves.csv --output waves.simdata --voxel 0.3

Requirements:
  pip install pandas numpy
"""

import struct
import argparse
import numpy as np
import pandas as pd
import os

MAGIC   = b'SIMD'
VERSION = 1

# ---------------------------------------------------------------------------
# Column config — matches StreamlineSystem.cs expected order:
# time, vx, vy, vz, id, x, y, z
# ---------------------------------------------------------------------------
COL_TIME = 0
COL_VX   = 1
COL_VY   = 2
COL_VZ   = 3
COL_ID   = 4
COL_X    = 5
COL_Y    = 6
COL_Z    = 7


def inspect(csv_path):
    """Print column names and a sample row so you can verify the format."""
    df = pd.read_csv(csv_path, nrows=3)
    print(f"\nFile: {csv_path}")
    print(f"Rows (approx): {sum(1 for _ in open(csv_path)) - 1:,}")
    print(f"Columns ({len(df.columns)}):")
    for i, col in enumerate(df.columns):
        print(f"  [{i:>2}]  {col:<30}  sample: {df[col].iloc[0]}")
    print()


def voxel_downsample(df, voxel_size):
    """
    Average points within each voxel cell.
    Operates per streamline ID so path integrity is preserved.
    """
    df = df.copy()
    df['_vx'] = np.floor(df.iloc[:, COL_X] / voxel_size).astype(int)
    df['_vy'] = np.floor(df.iloc[:, COL_Y] / voxel_size).astype(int)
    df['_vz'] = np.floor(df.iloc[:, COL_Z] / voxel_size).astype(int)

    id_col   = df.columns[COL_ID]
    group_by = [id_col, '_vx', '_vy', '_vz']

    agg = {
        df.columns[COL_X]:  'mean',
        df.columns[COL_Y]:  'mean',
        df.columns[COL_Z]:  'mean',
        df.columns[COL_VX]: 'mean',
        df.columns[COL_VY]: 'mean',
        df.columns[COL_VZ]: 'mean',
        df.columns[COL_TIME]: 'mean',
    }

    reduced = df.groupby(group_by, sort=False).agg(agg).reset_index()
    # Drop voxel index columns
    reduced = reduced.drop(columns=['_vx', '_vy', '_vz'])
    return reduced


def build_streamlines(df):
    """
    Group rows by streamline ID and return two dicts:
      paths[id]      -> list of (x, y, z)
      velocities[id] -> list of (vx, vy, vz)
    Mirrors the logic in StreamlineSystem.cs LoadCSV().
    """
    paths      = {}
    velocities = {}

    id_col = df.columns[COL_ID]

    for _, row in df.iterrows():
        sid = int(row.iloc[COL_ID])
        x, y, z    = float(row.iloc[COL_X]),  float(row.iloc[COL_Y]),  float(row.iloc[COL_Z])
        vx, vy, vz = float(row.iloc[COL_VX]), float(row.iloc[COL_VY]), float(row.iloc[COL_VZ])

        if sid not in paths:
            paths[sid]      = []
            velocities[sid] = []

        paths[sid].append((x, y, z))
        velocities[sid].append((vx, vy, vz))

    return paths, velocities


def write_simdata(output_path, paths, velocities):
    """Write the binary .simdata file."""
    ids = sorted(paths.keys())

    with open(output_path, 'wb') as f:
        # Header
        f.write(MAGIC)
        f.write(struct.pack('<i', VERSION))
        f.write(struct.pack('<i', len(ids)))

        total_points = 0

        for sid in ids:
            pts  = paths[sid]
            vels = velocities[sid]
            count = len(pts)
            f.write(struct.pack('<i', count))

            for i in range(count):
                x,  y,  z  = pts[i]
                vx, vy, vz = vels[i]
                f.write(struct.pack('<ffffff', x, y, z, vx, vy, vz))

            total_points += count

    size_kb = os.path.getsize(output_path) / 1024
    print(f"  Streamlines : {len(ids)}")
    print(f"  Total points: {total_points:,}")
    print(f"  Output size : {size_kb:.1f} KB  ->  {output_path}")


def convert(csv_path, output_path, voxel_size=None):
    print(f"\nReading: {csv_path}")
    df = pd.read_csv(csv_path)
    original_rows = len(df)
    print(f"  Rows loaded : {original_rows:,}")

    if voxel_size is not None and voxel_size > 0:
        df = voxel_downsample(df, voxel_size)
        reduced_rows = len(df)
        ratio = (1 - reduced_rows / original_rows) * 100
        print(f"  After voxel ({voxel_size}): {reduced_rows:,} rows  ({ratio:.1f}% reduction)")

    paths, velocities = build_streamlines(df)
    write_simdata(output_path, paths, velocities)
    print("Done.\n")


def parse_args():
    parser = argparse.ArgumentParser(
        description="Convert ParaView streamline CSVs to binary .simdata for Unity."
    )
    parser.add_argument('--input',   '-i', required=True, help="Input CSV file")
    parser.add_argument('--output',  '-o', default=None,  help="Output .simdata file (default: input name + .simdata)")
    parser.add_argument('--voxel',   '-v', type=float, default=None,
                        help="Voxel size for spatial downsampling. Omit to skip downsampling.")
    parser.add_argument('--inspect', action='store_true',
                        help="Print column names and exit (run this first).")
    return parser.parse_args()


if __name__ == '__main__':
    args = parse_args()

    if args.inspect:
        inspect(args.input)
    else:
        out = args.output or os.path.splitext(args.input)[0] + '.simdata'
        convert(args.input, out, args.voxel)
