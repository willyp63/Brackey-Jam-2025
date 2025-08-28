using System.Collections;
using UnityEngine;

public class ShakeBehavior : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField]
    private float defaultDuration = 0.3f;

    [SerializeField]
    private float defaultIntensity = 0.1f;

    [SerializeField]
    private float xFrequency = 50f;

    [SerializeField]
    private float yFrequency = 30f;

    [SerializeField]
    private float yIntensityMultiplier = 0.5f;

    private Vector3 originalPosition;

    private bool isShaking = false;
    public bool IsShaking => isShaking;

    public void Shake()
    {
        Shake(defaultDuration, defaultIntensity);
    }

    public void Shake(float duration, float intensity)
    {
        if (!isShaking)
        {
            StartCoroutine(
                ShakeCoroutine(duration, intensity, xFrequency, yFrequency, yIntensityMultiplier)
            );
        }
    }

    private IEnumerator ShakeCoroutine(
        float duration,
        float intensity,
        float xFreq,
        float yFreq,
        float yMultiplier
    )
    {
        originalPosition = transform.localPosition;
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float xOffset = Mathf.Sin(elapsed * xFreq) * intensity;
            float yOffset = Mathf.Cos(elapsed * yFreq) * intensity * yMultiplier;

            transform.localPosition = originalPosition + new Vector3(xOffset, yOffset, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isShaking = false;
    }
}
