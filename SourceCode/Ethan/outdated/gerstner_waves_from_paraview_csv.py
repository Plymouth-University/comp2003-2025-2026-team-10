"""
Gerstner Waves from ParaView CSV -> Unity JSON

Input
- CSV exported from ParaView using:
    Contour(alpha.water=0.5) -> Plot Over Line (vertical line at fixed x,z) ->
    Plot Data Over Time -> Save Data -> CSV

That CSV typically contains many rows per timestep (one per sample point on the line).
We will:
1) Group rows by time
2) Extract free-surface height eta(t) as the median of the Y coordinates at that timestep
3) Fit wave parameters:
   - amplitude A from peak-to-peak/2
   - dominant frequency f from FFT -> omega = 2πf
4) Choose/document the remaining parameters (direction + wavelength), or extend later
5) Export Unity JSON

Important clarification
- OpenFOAM has fields named 'k' and 'omega' in your folder. Those are turbulence fields.
- Here:
    k = wave number = 2π/L
    omega = wave angular frequency = 2πf
    
    
    RUN python -m pip install numpy
"""

import json
import math
from dataclasses import dataclass, asdict
from typing import Dict, List, Tuple

import numpy as np


@dataclass
class GerstnerWave:
    dir_x: float
    dir_z: float
    amplitude: float
    wavelength: float
    phase: float
    speed: float
    steepness: float
    k: float
    omega: float


def dominant_frequency_fft(t: np.ndarray, y: np.ndarray) -> float:
    """Dominant frequency (Hz) from FFT magnitude peak."""
    dt = float(np.median(np.diff(t)))
    y0 = y - np.mean(y)
    Y = np.fft.rfft(y0)
    freqs = np.fft.rfftfreq(len(y0), d=dt)
    mag = np.abs(Y)
    if len(mag) > 0:
        mag[0] = 0.0
    idx = int(np.argmax(mag))
    return float(freqs[idx])


def estimate_amplitude(y: np.ndarray) -> float:
    """Amplitude estimate: half peak-to-peak after mean removal."""
    y0 = y - np.mean(y)
    return 0.5 * float(np.max(y0) - np.min(y0))


def export_unity_json(waves: List[GerstnerWave], out_path: str, gravity: float = 9.81) -> None:
    payload = {"model": "gerstner", "gravity": gravity, "waves": [asdict(w) for w in waves]}
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2)


def load_csv_and_group_by_time(csv_path: str) -> Tuple[Dict[float, np.ndarray], List[str]]:
    """
    Loads a ParaView CSV and groups rows by timestep.

    Returns:
      grouped[time] = rows (N_rows_at_time, N_cols)
      header list
    """
    with open(csv_path, "r", encoding="utf-8", errors="ignore") as f:
        header = f.readline().strip().split(",")

    data = np.genfromtxt(csv_path, delimiter=",", skip_header=1)
    if data.ndim != 2 or data.shape[1] < 2:
        raise ValueError("CSV did not parse as a valid table.")

    # Locate time column (ParaView often uses 'Time')
    time_col = None
    for i, name in enumerate(header):
        if name.strip().lower() in ("time", "t"):
            time_col = i
            break
    if time_col is None:
        time_col = 0  # fallback

    grouped: Dict[float, List[np.ndarray]] = {}
    for row in data:
        tt = float(row[time_col])
        grouped.setdefault(tt, []).append(row)

    return {tt: np.vstack(rows) for tt, rows in grouped.items()}, header


def find_y_column(header: List[str]) -> int:
    """
    Find Y coordinate column. Common ParaView headers include:
      'Points:0', 'Points:1', 'Points:2'  (x,y,z)
    We want 'Points:1' (y).
    """
    lower = [h.strip().lower() for h in header]
    for i, h in enumerate(lower):
        if "points:1" in h:
            return i
    # If your CSV uses different naming, open the CSV and adjust this function.
    raise ValueError("Could not find Y coordinate column (expected something like 'Points:1').")


def extract_eta_time_series(csv_path: str) -> Tuple[np.ndarray, np.ndarray]:
    """
    For each timestep, the contour intersection points on the vertical line cluster around the surface height.
    We take the median Y value at each timestep as eta(t).
    """
    grouped, header = load_csv_and_group_by_time(csv_path)
    y_col = find_y_column(header)

    times = []
    etas = []

    for tt, rows in grouped.items():
        ys = rows[:, y_col]
        eta = float(np.median(ys))
        times.append(tt)
        etas.append(eta)

    order = np.argsort(times)
    t_sorted = np.array(times, dtype=float)[order]
    eta_sorted = np.array(etas, dtype=float)[order]
    return t_sorted, eta_sorted


def evidence_theta(x: float, z: float, t: float, w: GerstnerWave) -> Tuple[float, float, float]:
    """
    Evidence math for report:
      theta = k*dot(dir,(x,z)) + omega*t + phase
      uses sin(theta) and cos(theta)
    """
    d_dot_p = w.dir_x * x + w.dir_z * z
    theta = w.k * d_dot_p + w.omega * t + w.phase
    return theta, math.sin(theta), math.cos(theta)


if __name__ == "__main__":
    # Path to the ParaView CSV you saved
    csv_path = "paraview_line_over_time.csv"

    # 1) Extract eta(t) from ParaView export
    t, eta = extract_eta_time_series(csv_path)

    # 2) Fit dominant frequency + amplitude
    f0 = dominant_frequency_fft(t, eta)
    omega = 2.0 * math.pi * f0
    A = estimate_amplitude(eta)

    # 3) Remaining parameters (document these, or estimate later with more probes)
    wavelength = 10.0        # placeholder until you estimate crest spacing or two-probe lag
    k = 2.0 * math.pi / wavelength
    dir_x, dir_z = 1.0, 0.0  # placeholder direction
    phase = 0.0
    speed = 1.0
    steepness = 0.25

    wave = GerstnerWave(
        dir_x=dir_x,
        dir_z=dir_z,
        amplitude=A,
        wavelength=wavelength,
        phase=phase,
        speed=speed,
        steepness=steepness,
        k=k,
        omega=omega,
    )

    # 4) Export Unity JSON
    export_unity_json([wave], "waves_from_paraview.json", gravity=9.81)

    # 5) Print explicit theta/sin/cos evidence values
    mid_t = float(t[len(t) // 2])
    theta, s, c = evidence_theta(5.0, 2.0, mid_t, wave)
    print("Wrote waves_from_paraview.json")
    print("Evidence (theta, sin(theta), cos(theta)):", theta, s, c)
