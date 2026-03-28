using UnityEngine;
using UnityEngine.UI;

public class WorldHUDManager : MonoBehaviour
{
    [Header("Pet Stats UI")]
    public Text petNameText;
    public Text hungerText;
    public Text healthText;
    public Text happinessText;
    public Text energyText;
    public Text cleanlinessText;

    private void Start()
    {
        if (petNameText == null)
        {
            GameObject pnl = GameObject.Find("Pet Name Label");
            if (pnl != null) petNameText = pnl.GetComponent<Text>();
        }

        if (hungerText == null)
        {
            GameObject ht = GameObject.Find("Hunger Text");
            if (ht != null) hungerText = ht.GetComponent<Text>();
        }

        if (healthText == null)
        {
            GameObject ht = GameObject.Find("Health Text");
            if (ht != null) healthText = ht.GetComponent<Text>();
        }

        if (happinessText == null)
        {
            GameObject ht = GameObject.Find("Happiness Text");
            if (ht != null) happinessText = ht.GetComponent<Text>();
        }

        if (energyText == null)
        {
            GameObject ht = GameObject.Find("Energy Text");
            if (ht != null) energyText = ht.GetComponent<Text>();
        }

        if (cleanlinessText == null)
        {
            GameObject ht = GameObject.Find("Cleanliness Text");
            if (ht != null) cleanlinessText = ht.GetComponent<Text>();
        }
    }

    private void Update()
    {
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (petNameText != null)
        {
            petNameText.text = $"{GlobalData.PetName} (Dog)";
        }

        if (hungerText != null) hungerText.text = $"Hunger: {Mathf.RoundToInt(GlobalData.PetHunger)}%";
        if (healthText != null) healthText.text = $"Health: {Mathf.RoundToInt(GlobalData.PetHealth)}%";
        if (happinessText != null) happinessText.text = $"Happiness: {Mathf.RoundToInt(GlobalData.PetHappiness)}%";
        if (energyText != null) energyText.text = $"Energy: {Mathf.RoundToInt(GlobalData.PetEnergy)}%";
        if (cleanlinessText != null) cleanlinessText.text = $"Cleanliness: {Mathf.RoundToInt(GlobalData.PetCleanliness)}%";
    }
}
