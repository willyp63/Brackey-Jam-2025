using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private Player player;

    [SerializeField]
    private Light2D globalLight;

    [SerializeField]
    private float lightIntensity = 1f;

    private bool isGameOver = false;

    void Start()
    {
        globalLight.intensity = lightIntensity;
        player.onDeath += OnPlayerDeath;
    }

    void OnPlayerDeath()
    {
        EndGame(true);
    }

    public void EndGame(bool isDead)
    {
        isGameOver = true;
        Time.timeScale = 0f;
        UIManager.Instance.ShowGameOver(player.Gold, isDead);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetTimeScale(float timeScale)
    {
        if (isGameOver)
            return;

        Time.timeScale = timeScale;
    }
}
