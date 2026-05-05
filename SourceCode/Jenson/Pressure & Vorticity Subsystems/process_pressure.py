import os
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

GRID_RESOLUTION = 256
BLUR_SIGMA = 0.8


def find_col(header, key):
    for i, h in enumerate(header):
        if key.lower() in h.lower():
            return i
    raise ValueError(f"Column not found: '{key}'. Available: {header}")


def generate_pressuremaps(input_folder, output_folder):
    print("Generating pressure maps from surface pressure data...")

    csv_files = sorted([f for f in os.listdir(input_folder) if f.endswith(".csv")])

    if not csv_files:
        raise FileNotFoundError(f"No CSV files found in: {input_folder}")

    for i, filename in enumerate(csv_files):
        csv_path = os.path.join(input_folder, filename)
        out_path = os.path.join(output_folder, f"pressuremap_{i}.png")

        print(f"  [{i+1}/{len(csv_files)}] {filename}")

        with open(csv_path) as f:
            header = f.readline().strip().split(",")

        data = np.genfromtxt(csv_path, delimiter=",", skip_header=1)

        x_col = find_col(header, "Points:0")
        z_col = find_col(header, "Points:1")
        p_col = find_col(header, "p")

        x = data[:, x_col]
        z = data[:, z_col]
        p = data[:, p_col]

        xi = np.linspace(x.min(), x.max(), GRID_RESOLUTION)
        zi = np.linspace(z.min(), z.max(), GRID_RESOLUTION)

        grid = np.zeros((GRID_RESOLUTION, GRID_RESOLUTION))
        counts = np.zeros_like(grid)

        for n in range(len(p)):
            ix = np.searchsorted(xi, x[n]) - 1
            iz = np.searchsorted(zi, z[n]) - 1
            if 0 <= ix < GRID_RESOLUTION and 0 <= iz < GRID_RESOLUTION:
                grid[iz, ix] += p[n]
                counts[iz, ix] += 1

        counts[counts == 0] = 1
        grid /= counts

        low = np.percentile(grid, 2)
        high = np.percentile(grid, 98)
        grid = np.clip(grid, low, high)
        grid = (grid - low) / (high - low)

        grid = gaussian_filter(grid, sigma=BLUR_SIGMA)

        Image.fromarray((grid * 65535).astype(np.uint16)).save(out_path)

    print(f"Pressure maps done: {len(csv_files)} frames written to {output_folder}")
    return GRID_RESOLUTION
