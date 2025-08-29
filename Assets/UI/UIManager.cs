using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
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

    public void Start()
    {
        player.onHealthChanged += UpdateHealthUI;
        player.onGoldChanged += UpdateGoldUI;
        UpdateEnergyUI();
        UpdateHealthUI();
        UpdateGoldUI();
    }

    public void Update()
    {
        UpdateEnergyUI();
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
}
