using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField]
    private Player player;

    [SerializeField]
    private List<Image> healthFillImages;

    [SerializeField]
    private List<Image> healthFrameImages;

    [SerializeField]
    private List<Image> energyFillImages;

    [SerializeField]
    private List<Image> energyFrameImages;

    [SerializeField]
    private List<TextMeshProUGUI> goldTexts;

    [SerializeField]
    private List<TextMeshProUGUI> clockTexts;

    [Header("Game Over")]
    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private List<TextMeshProUGUI> gameOverScoreTexts;

    [SerializeField]
    private Button restartButton;

    private float startTime;

    public void Start()
    {
        restartButton.onClick.AddListener(RestartGame);
        gameOverPanel.SetActive(false);

        player.onHealthChanged += UpdateHealthUI;
        player.onGoldChanged += UpdateGoldUI;
        UpdateEnergyUI();
        UpdateHealthUI();
        UpdateGoldUI();

        startTime = Time.time;
    }

    public void Update()
    {
        UpdateEnergyUI();
        UpdateClockUI();
    }

    void RestartGame()
    {
        GameManager.Instance.RestartGame();
    }

    public void ShowGameOver(int finalScore, bool isDead)
    {
        gameOverPanel.SetActive(true);
        for (int i = 0; i < gameOverScoreTexts.Count; i++)
        {
            gameOverScoreTexts[i].text = isDead ? "YOU DIED" : $"YOU GAINED\n{finalScore} GOLD";
        }
    }

    public void UpdateGoldUI()
    {
        for (int i = 0; i < goldTexts.Count; i++)
        {
            goldTexts[i].text = player.Gold.ToString("N0");
        }
    }

    public void UpdateEnergyUI()
    {
        for (int i = 0; i < energyFrameImages.Count; i++)
        {
            energyFrameImages[i].enabled = i == player.MaxEnergy - 1;
        }

        for (int i = 0; i < energyFillImages.Count; i++)
        {
            energyFillImages[i].enabled = i == player.MaxEnergy - 1;
            energyFillImages[i].fillAmount = player.Energy / player.MaxEnergy;
        }
    }

    private void UpdateHealthUI()
    {
        for (int i = 0; i < healthFrameImages.Count; i++)
        {
            healthFrameImages[i].enabled = i == player.MaxHealth - 1;
        }

        for (int i = 0; i < healthFillImages.Count; i++)
        {
            healthFillImages[i].enabled = i < player.Health;
        }
    }

    private void UpdateClockUI()
    {
        for (int i = 0; i < clockTexts.Count; i++)
        {
            int numSeconds = (int)(Time.time - startTime);
            int minutes = numSeconds / 60;
            int seconds = numSeconds % 60;
            clockTexts[i].text = $"{minutes:D2}:{seconds:D2}";
        }
    }
}
