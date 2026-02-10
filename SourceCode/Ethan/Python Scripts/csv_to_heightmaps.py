import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

csv_path = "surface.csv"
output_png = "heightmap.png"
grid_resolution = 256
blur_sigma = 0.8

data = np.genfromtxt(csv_path, delimiter=",", skip_header=1)

# Find columns
with open(csv_path) as f:
    header = f.readline().strip().split(",")

def find_col(name):
    for i, h in enumerate(header):
        if name.lower() in h.lower():
            return i
    raise ValueError(f"Column not found: {name}")

x_col = find_col("Points:0")
z_col = find_col("Points:1")
h_col = find_col("Points:2")  # water height

x = data[:, x_col]
z = data[:, z_col]
h = data[:, h_col]

# Build grid
xi = np.linspace(x.min(), x.max(), grid_resolution)
zi = np.linspace(z.min(), z.max(), grid_resolution)

grid = np.zeros((grid_resolution, grid_resolution))
counts = np.zeros_like(grid)

for i in range(len(h)):
    ix = np.searchsorted(xi, x[i]) - 1
    iz = np.searchsorted(zi, z[i]) - 1

    if 0 <= ix < grid_resolution and 0 <= iz < grid_resolution:
        grid[iz, ix] += h[i]
        counts[iz, ix] += 1

counts[counts == 0] = 1
grid /= counts

# Normalize using percentiles (prevents spikes)
low = np.percentile(grid, 2)
high = np.percentile(grid, 98)
grid = np.clip(grid, low, high)
grid = (grid - low) / (high - low)

# Smooth
grid = gaussian_filter(grid, sigma=blur_sigma)

# Convert to 16-bit image
img = (grid * 65535).astype(np.uint16)
Image.fromarray(img).save(output_png)

print("Height map written:", output_png)
print("Min:", grid.min(), "Max:", grid.max())

