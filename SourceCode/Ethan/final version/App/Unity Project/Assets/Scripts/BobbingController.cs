using UnityEngine;

public class BobbingController : MonoBehaviour
{
    [Header("Bobbing Settings")]
    public float bobFrequency = 1f;
    public float bobAmplitude = 0.1f;

    private FOWTController fowt;

    void Start()
    {
        fowt = GetComponent<FOWTController>();

        if (fowt == null)
            Debug.LogError("BobbingController: FOWTController not found on this GameObject.");
    }

    void Update()
    {
        if (fowt == null) return;

        fowt.bobbingOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
    }
}
