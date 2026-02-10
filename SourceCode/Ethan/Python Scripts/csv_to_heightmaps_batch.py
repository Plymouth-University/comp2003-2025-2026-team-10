import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter
import os

grid_resolution = 256
blur_sigma = 0.8

def find_col(header, key):
    for i, h in enumerate(header):
        if key.lower() in h.lower():
            return i
    raise ValueError(f"Missing column: {key}")

for i in range(11):
    csv_path = f"waves_{i}.csv"
    output_png = f"heightmap_{i}.png"

    if not os.path.exists(csv_path):
        print(f"Skipping missing file: {csv_path}")
        continue

    print("Processing:", csv_path)

    with open(csv_path) as f:
        header = f.readline().strip().split(",")

    data = np.genfromtxt(csv_path, delimiter=",", skip_header=1)

    x_col = find_col(header, "Points:0")
    z_col = find_col(header, "Points:1")
    h_col = find_col(header, "Points:2")  # water height (Z)

    x = data[:, x_col]
    z = data[:, z_col]
    h = data[:, h_col]

    xi = np.linspace(x.min(), x.max(), grid_resolution)
    zi = np.linspace(z.min(), z.max(), grid_resolution)

    grid = np.zeros((grid_resolution, grid_resolution))
    counts = np.zeros_like(grid)

    for n in range(len(h)):
        ix = np.searchsorted(xi, x[n]) - 1
        iz = np.searchsorted(zi, z[n]) - 1

        if 0 <= ix < grid_resolution and 0 <= iz < grid_resolution:
            grid[iz, ix] += h[n]
            counts[iz, ix] += 1

    counts[counts == 0] = 1
    grid /= counts

    # percentile normalization (prevents spikes)
    low = np.percentile(grid, 2)
    high = np.percentile(grid, 98)
    grid = np.clip(grid, low, high)
    grid = (grid - low) / (high - low)

    # smooth turbulence noise slightly
    grid = gaussian_filter(grid, sigma=blur_sigma)

    img = (grid * 65535).astype(np.uint16)
    Image.fromarray(img).save(output_png)

    print("Saved:", output_png)

print("DONE")

