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

    t_min = timesteps[0]["time"]
    t_max = timesteps[-1]["time"]

    # FIX: clamp query_time to valid range instead of raising an error,
    # so callers passing slightly out-of-range times get the boundary value
    # rather than a hard crash.
    if query_time <= t_min:
        return list(timesteps[0]["values"])
    if query_time >= t_max:
        return list(timesteps[-1]["values"])

    for i in range(len(timesteps) - 1):
        t0 = timesteps[i]
        t1 = timesteps[i + 1]

        if t0["time"] <= query_time <= t1["time"]:
            span = t1["time"] - t0["time"]
            if span <= 0:
                raise ValueError(f"Invalid timestep ordering at index {i}: span={span}")
            alpha = (query_time - t0["time"]) / span
            return interpolate_values(t0["values"], t1["values"], alpha)

    # Should never reach here after clamping, but kept as a safety net
    raise ValueError(f"query_time={query_time} fell through interpolation loop unexpectedly.")


if __name__ == "__main__":
    with open("temporal_interpolation_example.json", "r", encoding="utf-8") as f:
        dataset = json.load(f)

    print("Temporal Interpolation Runtime Demo")
    print("Field:", dataset.get("field"))

    t_min = dataset["timesteps"][0]["time"]
    t_max = dataset["timesteps"][-1]["time"]

    # FIX: demo now queries times that are actually within the dataset range,
    # and also tests clamping behaviour at the boundaries.
    test_times = [t_min, 0.5, 1.0, 1.5, t_max, t_max + 0.5]
    for query_time in test_times:
        result = interpolate_at_time(dataset, query_time)
        rounded = [round(v, 4) for v in result]
        print(f"  t={query_time:.2f} -> {rounded}")
