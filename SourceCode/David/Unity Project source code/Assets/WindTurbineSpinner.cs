using UnityEngine;

public class WindTurbineSpinner : MonoBehaviour
{
    // Public variable to control rotation speed (in degrees per second)
    public float rotationSpeed = 100f;

    // Axis of rotation (default is Z-axis, adjust if needed)
    public Vector3 rotationAxis = new Vector3(0, 0, 1);

    void Update()
    {
        // Rotate the turbine every frame
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
}