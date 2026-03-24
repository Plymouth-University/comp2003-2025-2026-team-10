import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter
import os

GRID_RESOLUTION = 256
BLUR_SIGMA = 0.8


def find_col(header, key):
    for i, h in enumerate(header):
        if key.lower() in h.lower():
            return i
    raise ValueError(f"Missing column: {key}")


def process_single_csv(csv_path, output_path):
    with open(csv_path) as f:
        header = f.readline().strip().split(",")

    data = np.genfromtxt(csv_path, delimiter=",", skip_header=1)

    x_col = find_col(header, "Points:0")
    z_col = find_col(header, "Points:1")
    h_col = find_col(header, "Points:2")

    x = data[:, x_col]
    z = data[:, z_col]
    h = data[:, h_col]

    xi = np.linspace(x.min(), x.max(), GRID_RESOLUTION)
    zi = np.linspace(z.min(), z.max(), GRID_RESOLUTION)

    grid = np.zeros((GRID_RESOLUTION, GRID_RESOLUTION))
    counts = np.zeros_like(grid)

    for n in range(len(h)):
        ix = np.searchsorted(xi, x[n]) - 1
        iz = np.searchsorted(zi, z[n]) - 1

        if 0 <= ix < GRID_RESOLUTION and 0 <= iz < GRID_RESOLUTION:
            grid[iz, ix] += h[n]
            counts[iz, ix] += 1

    counts[counts == 0] = 1
    grid /= counts

    low = np.percentile(grid, 2)
    high = np.percentile(grid, 98)
    grid = np.clip(grid, low, high)
    grid = (grid - low) / (high - low)

    grid = gaussian_filter(grid, sigma=BLUR_SIGMA)

    img = (grid * 65535).astype(np.uint16)
    Image.fromarray(img).save(output_path)


def generate_heightmaps(input_folder, output_folder):
    files = sorted([f for f in os.listdir(input_folder) if f.endswith(".csv")])

    if len(files) == 0:
        raise Exception("No CSV files found for water")

    print(f"Processing {len(files)} water frames...")

    for i, file in enumerate(files):
        csv_path = os.path.join(input_folder, file)
        output_path = os.path.join(output_folder, f"heightmap_{i}.png")

        print(f"[{i}] {file}")
        process_single_csv(csv_path, output_path)

    print("Water processing complete")

    return GRID_RESOLUTION