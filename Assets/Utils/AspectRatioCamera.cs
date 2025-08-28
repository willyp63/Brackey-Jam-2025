using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AspectRatioCamera : MonoBehaviour
{
    [SerializeField]
    private float targetAspect = 16.0f / 9.0f;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateCameraRect();
    }

    void Update()
    {
        // Check if screen size changed (useful for desktop builds)
        UpdateCameraRect();
    }

    void UpdateCameraRect()
    {
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            // Letterboxing (black bars on top and bottom)
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            cam.rect = rect;
        }
        else
        {
            // Pillarboxing (black bars on left and right)
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }
    }
}
