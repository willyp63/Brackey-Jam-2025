using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private Light2D globalLight;

    [SerializeField]
    private float lightIntensity = 1f;

    void Start()
    {
        globalLight.intensity = lightIntensity;
    }
}
