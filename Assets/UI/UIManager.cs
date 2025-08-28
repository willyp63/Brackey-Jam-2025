using System.Collections;
using System.Collections.Generic;
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

    public void Start()
    {
        player.onHealthChanged += UpdateHealthUI;
        UpdateHealthUI();
        UpdateEnergyUISprites();
    }

    public void Update()
    {
        UpdateEnergyUI();
    }

    public void UpdateEnergyUI()
    {
        energyFillImages[player.MaxEnergy - 1].fillAmount = player.Energy / player.MaxEnergy;
    }

    public void UpdateEnergyUISprites()
    {
        for (int i = 0; i < energyFrameImages.Count; i++)
        {
            energyFrameImages[i].enabled = i == player.MaxEnergy - 1;
        }

        for (int i = 0; i < energyFillImages.Count; i++)
        {
            energyFillImages[i].enabled = i == player.MaxEnergy - 1;
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
