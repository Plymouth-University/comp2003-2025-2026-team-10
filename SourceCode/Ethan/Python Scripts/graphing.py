import pandas as pd
import matplotlib.pyplot as plt

df = pd.read_csv("surface.csv")
times = sorted(df["Time"].unique())

for t in times:
    df_t = df[df["Time"] == t]
    plt.clf()
    plt.scatter(df_t["Points:0"], df_t["WaterHeight"], s=1)
    plt.title(f"Time {t}")
    plt.pause(0.5)

plt.show()

