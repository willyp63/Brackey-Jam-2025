using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField]
    private float parallaxEffectX = 0.5f;

    [SerializeField]
    private float parallaxEffectY = 0.5f;

    [Header("References")]
    [SerializeField]
    private Camera mainCamera;

    private float startPosX;
    private float startPosY;
    private Vector3 cameraStartPos;

    void Start()
    {
        // Auto-find camera if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "No camera found! Please assign a camera or ensure there's a Main Camera in the scene."
            );
            return;
        }

        // Store initial positions
        startPosX = transform.position.x;
        startPosY = transform.position.y;
        cameraStartPos = mainCamera.transform.position;
    }

    void Update()
    {
        if (mainCamera == null)
            return;

        // Calculate how far the camera has moved
        float distanceX = mainCamera.transform.position.x - cameraStartPos.x;
        float distanceY = mainCamera.transform.position.y - cameraStartPos.y;

        // Calculate the parallax positions
        float targetPosX = startPosX + (distanceX * parallaxEffectX);
        float targetPosY = startPosY + (distanceY * parallaxEffectY);

        // Apply the parallax movement
        Vector3 newPos = transform.position;
        newPos.x = targetPosX;
        newPos.y = targetPosY;
        transform.position = newPos;
    }

    // Optional: Method to manually set the X parallax effect
    public void SetParallaxEffectX(float newEffect)
    {
        parallaxEffectX = newEffect;
    }

    // Optional: Method to manually set the Y parallax effect
    public void SetParallaxEffectY(float newEffect)
    {
        parallaxEffectY = newEffect;
    }
}
