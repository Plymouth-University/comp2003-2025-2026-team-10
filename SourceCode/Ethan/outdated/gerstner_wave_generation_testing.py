"""
Gerstner Wave Generation (Testing) -> Unity JSON

Purpose
- Generate a reproducible set of Gerstner wave parameters.
- Export them as a Unity-friendly JSON file.
- This is for testing the Unity pipeline (load JSON -> render procedural ocean).

What Unity does with this JSON
- Unity reads the JSON once at startup.
- Unity then computes wave displacement every frame (shader preferred, C# acceptable).
- No CFD input here: parameters are plausible test values only.
"""

import json
import math
import random
from dataclasses import dataclass, asdict
from typing import List, Tuple


@dataclass
class GerstnerWave:
    # Direction of travel in the horizontal plane (unit vector on XZ).
    dir_x: float
    dir_z: float

    # Amplitude (meters): controls wave height contribution.
    amplitude: float

    # Wavelength (meters): distance between wave crests.
    wavelength: float

    # Phase (radians): offsets where the wave starts (randomizes alignment).
    phase: float

    # Speed multiplier: scales dispersion speed (1.0 = deep-water dispersion).
    speed: float

    # Steepness: controls sharpness/peakedness (keep modest to avoid artifacts).
    steepness: float


def unit_dir_from_angle(theta_rad: float) -> Tuple[float, float]:
    """Convert an angle in radians into a unit direction vector on the XZ plane."""
    return math.cos(theta_rad), math.sin(theta_rad)


def generate_waves(
    wave_count: int = 6,
    seed: int = 42,
    amplitude_range: Tuple[float, float] = (0.08, 0.30),
    wavelength_range: Tuple[float, float] = (3.0, 16.0),
    steepness_range: Tuple[float, float] = (0.12, 0.35),
    speed_range: Tuple[float, float] = (0.9, 1.2),
    prevailing_direction_degrees: float = 30.0,
    direction_spread_degrees: float = 35.0,
) -> List[GerstnerWave]:
    """
    Realistic-looking water is a sum of multiple waves.
    This function generates N different wave components, each with slightly different parameters.
    """
    rng = random.Random(seed)

    prevailing = math.radians(prevailing_direction_degrees)
    spread = math.radians(direction_spread_degrees)

    waves: List[GerstnerWave] = []

    for _ in range(wave_count):
        # Pick a direction near a prevailing direction (so waves don't look chaotic)
        theta = prevailing + rng.uniform(-spread, spread)
        dx, dz = unit_dir_from_angle(theta)

        # Pick plausible parameters
        amp = rng.uniform(*amplitude_range)
        wl = rng.uniform(*wavelength_range)
        st = rng.uniform(*steepness_range)
        sp = rng.uniform(*speed_range)
        ph = rng.uniform(0.0, 2.0 * math.pi)

        waves.append(
            GerstnerWave(
                dir_x=dx,
                dir_z=dz,
                amplitude=amp,
                wavelength=wl,
                phase=ph,
                speed=sp,
                steepness=st,
            )
        )

    return waves


def export_unity_json(waves: List[GerstnerWave], out_path: str = "waves_testing.json", gravity: float = 9.81) -> None:
    """
    Output format is intentionally simple: just floats and arrays.

    Unity side:
    - Load JSON -> wave list
    - For each vertex/world position (x,z) at time t:
        k = 2π / wavelength
        omega = sqrt(g*k) * speed
        theta = k * dot(dir, (x,z)) + omega*t + phase
        y += amplitude * sin(theta)
      Optional Gerstner horizontal offsets use cos(theta) too.
    """
    payload = {
        "model": "gerstner",
        "gravity": gravity,
        "waves": [asdict(w) for w in waves],
    }
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2)


# Optional: reference function for quick validation outside Unity
def height_at(x: float, z: float, t: float, waves: List[GerstnerWave], gravity: float = 9.81) -> float:
    """
    Vertical displacement:
      y(x,z,t) = Σ A * sin(k*dot(dir,p) + ω*t + phase)

    k (wave number) = 2π / wavelength
    ω (angular frequency) = sqrt(g*k) scaled by speed
    """
    y = 0.0
    for w in waves:
        k = 2.0 * math.pi / max(w.wavelength, 1e-9)
        omega = math.sqrt(gravity * k) * w.speed
        theta = k * (w.dir_x * x + w.dir_z * z) + omega * t + w.phase
        y += w.amplitude * math.sin(theta)
    return y


if __name__ == "__main__":
    waves = generate_waves()
    export_unity_json(waves, out_path="waves_testing.json")
    print("Wrote waves_testing.json with", len(waves), "waves.")
