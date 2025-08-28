using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [SerializeField]
    private Light2D globalLight;

    [SerializeField]
    private float lightIntensity = 1f;

    void Start()
    {
        globalLight.intensity = lightIntensity;
        player.onDeath += OnPlayerDeath;
    }

    void OnPlayerDeath()
    {
        Debug.Log("Player died");

        StartCoroutine(RestartGame());
    }

    IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
