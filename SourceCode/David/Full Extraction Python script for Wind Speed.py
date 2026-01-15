import pyvista as pv
import numpy as np

# Use the coordinates we just calculated
hub_center = [7.824, 0.0, 2.135]

# Your 10 snapshot folder names
snapshots = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"]

print("Starting Extraction...")

for folder in snapshots:
    # 1. GET BLADE SPEED (Targeting InternalMesh + U)
    # We read the 'U' file specifically
    air_data = pv.read(f"{folder}/U")
    probe = air_data.probe(np.array([hub_center]))
    velocity_vec = probe["U"][0]
    wind_speed = np.linalg.norm(velocity_vec)

    # 2. GET PLATFORM MOTION (Targeting FOWT_platform + pointDisplacement)
    # We read the pointDisplacement file specifically
    platform_data = pv.read(f"{folder}/pointDisplacement")
    # Average displacement of the whole platform
    avg_displacement = np.mean(platform_data["pointDisplacement"], axis=0)

    print(f"Snapshot {folder}: Wind={wind_speed:.2f} m/s | Heave={avg_displacement[2]:.4f} m")

print("Extraction Complete.")
