using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    [Header("Oscillation Settings")]
    [SerializeField]
    private float amplitude = 1f; // How far up/down to move

    [SerializeField]
    private float frequency = 2f; // How fast to oscillate

    [SerializeField]
    private Vector3 oscillationAxis = Vector3.up; // Which axis to oscillate on

    private Vector3 startPosition;

    void Start()
    {
        // Store the initial position
        startPosition = transform.position;
    }

    void Update()
    {
        // Calculate the sine wave offset
        float sineValue = Mathf.Sin(Time.time * frequency);

        // Apply the oscillation to the position
        Vector3 offset = oscillationAxis * (sineValue * amplitude);
        transform.position = startPosition + offset;
    }
}
