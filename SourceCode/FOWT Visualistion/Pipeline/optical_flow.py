import cv2
import numpy as np
import os
import json
import sys

_HERE      = os.path.dirname(os.path.abspath(__file__))
INPUT_DIR  = sys.argv[1] if len(sys.argv) > 1 else os.path.join(_HERE, 'Simulation_Input', 'water', 'heightmaps')
OUTPUT_DIR = sys.argv[2] if len(sys.argv) > 2 else os.path.join(_HERE, 'FOWT Visualisation Simulation', 'VAWT Turbine simulation_Data', 'StreamingAssets', 'flowmaps')

os.makedirs(OUTPUT_DIR, exist_ok=True)

# Get all PNGs sorted numerically
frames = sorted(
    [f for f in os.listdir(INPUT_DIR) if f.endswith('.png')],
    key=lambda x: int(x.replace('heightmap_', '').replace('.png', ''))
)

frame_count = len(frames)
print(f"Found {frame_count} frames")
print("Order check:", frames[:5])  # prints first 5 so we can confirm order

def load_heightmap(path):
    img = cv2.imread(path, cv2.IMREAD_GRAYSCALE)
    if img is None:
        raise FileNotFoundError(f"Could not load: {path}")
    return img.astype(np.float32) / 255.0

for i in range(frame_count - 1):
    path_a = os.path.join(INPUT_DIR, frames[i])
    path_b = os.path.join(INPUT_DIR, frames[i + 1])

    frame_a = load_heightmap(path_a)
    frame_b = load_heightmap(path_b)

    a_uint8 = (frame_a * 255).astype(np.uint8)
    b_uint8 = (frame_b * 255).astype(np.uint8)

    flow = cv2.calcOpticalFlowFarneback(
        a_uint8, b_uint8,
        flow=None,
        pyr_scale=0.5,
        levels=4,
        winsize=16,
        iterations=3,
        poly_n=7,
        poly_sigma=1.5,
        flags=0
    )

    output_path = os.path.join(OUTPUT_DIR, f'flow_{i:04d}.exr')
    cv2.imwrite(output_path, flow)
    print(f'Flow {i} → {i+1} done ({frames[i]} → {frames[i+1]})')

metadata = {
    'frame_count': frame_count,
    'flow_count': frame_count - 1,
    'frames': frames
}
with open(os.path.join(OUTPUT_DIR, 'metadata.json'), 'w') as f:
    json.dump(metadata, f, indent=2)

print(f'\nDone. {frame_count - 1} flow maps saved to {OUTPUT_DIR}')
