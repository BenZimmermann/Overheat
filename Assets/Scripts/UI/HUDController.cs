using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
public class HUDController : MonoBehaviour
{
    /// <summary>
    /// Gettet events and shows the player stats, upgrades, health and ammo on the HUD. 
    /// This is a singleton class that can be accessed from anywhere in the code to update the HUD. 
    /// It should be refactored to use events and listeners for better performance and decoupling.
    /// </summary>
    //public static HUDController Instance { get; set; }
    //[Header("Stats")]
    //[SerializeField] public TextMeshProUGUI LevelName;
    //[SerializeField] public TextMeshProUGUI Time;
    //[SerializeField] public TextMeshProUGUI EnemiesKilled;
    //[SerializeField] public TextMeshProUGUI Money;
    //[SerializeField] public TextMeshProUGUI FinalScore;

    //[Header("Upgrades")]
    //[SerializeField] public GameObject[] Upgrades;

    //[Header("Health")]
    //[SerializeField] public Slider HealthBar;
    //[SerializeField] public TextMeshProUGUI HealthText;
    //[SerializeField] public Slider Shild;
    //[SerializeField] public GameObject Item;

    [Header("Ammo")]
    [SerializeField] public TextMeshProUGUI Ammo;

    //Temporary method to update the HUD, should be replaced with events and listeners for better performance and decoupling
    private void Start()
    {
        FindAnyObjectByType<ShootController>()?.GetComponent<ShootController>();

    }
    private void Update()
    {
        if (FindAnyObjectByType<ShootController>() != null)
        {
            ShootController shootController = FindAnyObjectByType<ShootController>();
            Ammo.text = $"{shootController.CurrentAmmo} / {shootController.MagazineSize}";
            if (shootController.IsReloading)
            {
                Ammo.text = "Reloading...";
            }
        }
    }
    private void UpdateHealth(float currentHealth, float maxHealth)
    {
    }
    private void UpdateShield(float currentShield, float maxShield)
    {
    }
    private void UpdateStats(string levelName, float time, int enemiesKilled, int money, int finalScore)
    {
    }
    private void UpdateUpgrades(List<string> upgrades)
    {
    }
    private void UpdateItem(string itemName)
    {
    }
    private void UpdateAmmo(int currentAmmo, int magazineSize, bool isReloading)
    {
    }
}
