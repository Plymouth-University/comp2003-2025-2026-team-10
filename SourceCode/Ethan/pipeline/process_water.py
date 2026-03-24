def generate_heightmaps(csv_file, output_folder):

    print("Generating heightmaps from water surface data...")

    def generate_heightmaps(input_folder, output_folder):

    import numpy as np
    from PIL import Image
    from scipy.ndimage import gaussian_filter
    import os

    print("Generating heightmaps from water surface data...")

    grid_resolution = 256
    blur_sigma = 0.8

    def find_col(header, key):
        for i, h in enumerate(header):
            if key.lower() in h.lower():
                return i
        raise ValueError(f"Missing column: {key}")

    # get all CSV files dynamically
    csv_files = sorted([f for f in os.listdir(input_folder) if f.endswith(".csv")])

    if len(csv_files) == 0:
        raise Exception("No CSV files found in water input folder")

    for i, file in enumerate(csv_files):
        csv_path = os.path.join(input_folder, file)
        output_png = os.path.join(output_folder, f"heightmap_{i}.png")

        print("Processing:", csv_path)

        with open(csv_path) as f:
            header = f.readline().strip().split(",")

        data = np.genfromtxt(csv_path, delimiter=",", skip_header=1)

        x_col = find_col(header, "Points:0")
        z_col = find_col(header, "Points:1")
        h_col = find_col(header, "Points:2")

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

        low = np.percentile(grid, 2)
        high = np.percentile(grid, 98)
        grid = np.clip(grid, low, high)
        grid = (grid - low) / (high - low)

        grid = gaussian_filter(grid, sigma=blur_sigma)

        img = (grid * 65535).astype(np.uint16)
        Image.fromarray(img).save(output_png)

        print("Saved:", output_png)

    print("Heightmaps generated.")

    return grid_resolution
    print("Heightmaps generated.")