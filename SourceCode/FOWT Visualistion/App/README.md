# FOWT Visualisation — App

Ready-to-run builds for Windows and Linux. Just open the application and it works — no setup, no Unity, no pipeline required.

The visualisation renders three components simultaneously from the CFD simulation data:


---

## Running the app

- **Windows** — open `Windows Version/` and launch the `.exe`
- **Linux** — open `Linux Version/` and launch the application binary

No installation or configuration required.

---

## Simulation data included

Both builds come pre-loaded with the full FOWT simulation dataset covering **timesteps 30–65**.

The raw OpenFOAM CFD output for this dataset is approximately **220 GB**. The processed data bundled here is **under 1 GB**, achieved by converting the simulation data into compressed heightmaps and streamline CSVs — a fraction of the original size with a fraction of the computational requirements.

---