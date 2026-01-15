import json
from typing import List, Dict, Any

def lerp(a: float, b: float, alpha: float) -> float:
    return (1.0 - alpha) * a + alpha * b


def interpolate_values(values_a: List[float], values_b: List[float], alpha: float) -> List[float]:
    if len(values_a) != len(values_b):
        raise ValueError("Cannot interpolate: values arrays differ in length.")
    return [lerp(a, b, alpha) for a, b in zip(values_a, values_b)]


def interpolate_at_time(dataset: Dict[str, Any], query_time: float) -> List[float]:
    timesteps = dataset.get("timesteps", [])
    if len(timesteps) < 2:
        raise ValueError("Dataset must contain at least two timesteps.")

    timesteps = sorted(timesteps, key=lambda ts: ts["time"])

    for i in range(len(timesteps) - 1):
        t0 = timesteps[i]
        t1 = timesteps[i + 1]

        if t0["time"] <= query_time <= t1["time"]:
            span = t1["time"] - t0["time"]
            if span <= 0:
                raise ValueError("Invalid timestep ordering.")
            alpha = (query_time - t0["time"]) / span
            return interpolate_values(t0["values"], t1["values"], alpha)

    raise ValueError("Requested time is outside the stored timestep range.")


if __name__ == "__main__":
    with open("temporal_interpolation_example.json", "r", encoding="utf-8") as f:
        dataset = json.load(f)

    print("Temporal Interpolation Runtime Demo")
    print("Field:", dataset.get("field"))

    for query_time in (0.5, 1.5):
        reconstructed = interpolate_at_time(dataset, query_time)
        print(f"t={query_time:.2f} -> {reconstructed}")
