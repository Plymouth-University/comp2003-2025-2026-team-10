public class DataInterpolator : MonoBehaviour
{
    public float[] snapshotData; // Your 10 values (e.g., RPM or Wind Speed)
    public float timeBetweenSnapshots = 1.0f; // 1 second per snapshot
    private float currentTime = 0f;

    void Update()
    {
        currentTime += Time.deltaTime;

        // Calculate which snapshots we are between
        int indexA = Mathf.FloorToInt(currentTime / timeBetweenSnapshots);
        int indexB = indexA + 1;

        // Loop back to the start if we reach the end of the 10 snapshots
        if (indexB >= snapshotData.Length) {
            currentTime = 0;
            return;
        }

        // Calculate 't' (the fraction of progress between A and B)
        float t = (currentTime % timeBetweenSnapshots) / timeBetweenSnapshots;

        // Interpolate the value
        float interpolatedValue = Mathf.Lerp(snapshotData[indexA], snapshotData[indexB], t);

        // Apply it to the turbine (e.g., rotation speed)
        transform.Rotate(Vector3.up, interpolatedValue * Time.deltaTime);
    }
}
