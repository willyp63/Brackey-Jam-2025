using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private Light2D globalLight;

    void Start()
    {
        globalLight.intensity = 0f;
    }
}
