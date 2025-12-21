using UnityEngine;

public class ObjectFrameAnimator : MonoBehaviour
{
    public GameObject[] frames;
    public float frameRate = 10f;

    private int currentFrame;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0f;

            frames[currentFrame].SetActive(false);
            currentFrame = (currentFrame + 1) % frames.Length;
            frames[currentFrame].SetActive(true);
        }
    }
}
