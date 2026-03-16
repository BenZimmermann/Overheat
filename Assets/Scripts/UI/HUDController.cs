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
    [Header("Stats")]
    //[SerializeField] public TextMeshProUGUI LevelName;
    [SerializeField] public TextMeshProUGUI RunTime;
    [SerializeField] public TextMeshProUGUI EnemiesKilled;
    [SerializeField] public TextMeshProUGUI Money;
    [SerializeField] public TextMeshProUGUI FinalScore;

    [Header("Upgrades")]
    [SerializeField] public GameObject UpgradeHolder;
    [SerializeField] private GameObject _upgradeIconPrefab;

    //[Header("Health")]
    [SerializeField] public Slider HealthBar;
    [SerializeField] public TextMeshProUGUI HealthText;
    [SerializeField] public Slider ShieldBar;
    [SerializeField] public TextMeshProUGUI ShieldText;
    [SerializeField] public GameObject Item;
    [SerializeField] private Sprite DefaultItemSprite;

    [Header("UI Panels")]
    public GameObject healthUI;
    public GameObject statsUI;
    public GameObject upgradesUI;

    [Header("Ammo")]
    [SerializeField] public TextMeshProUGUI Ammo;

    [Header("GameOver")]
    [SerializeField] public GameObject GameOverMenu;

    void OnEnable()
    {
        SettingsManager.Instance.OnSettingsChanged += ApplySettings;
        GameManager.Instance.Data.OnDataChanged += RefreshStats;
        GameManager.Instance.Data.OnUpgradeAdded += RefreshUpgrade;
        GameManager.Instance.Data.OnItemChanged += RefreshItem;
        GameManager.Instance.OnGameOver += RefreshGameOver;

    }

    void OnDisable()
    {
        SettingsManager.Instance.OnSettingsChanged -= ApplySettings;
        GameManager.Instance.Data.OnDataChanged -= RefreshStats;
        GameManager.Instance.Data.OnUpgradeAdded -= RefreshUpgrade;
        GameManager.Instance.Data.OnItemChanged -= RefreshItem;
        GameManager.Instance.OnGameOver -= RefreshGameOver;
    }
    //Temporary method to update the HUD, should be replaced with events and listeners for better performance and decoupling

    private void Start()
    {

        FindAnyObjectByType<ShootController>()?.GetComponent<ShootController>();
        FindAnyObjectByType<MeleeController>()?.GetComponent<MeleeController>();
        ApplySettings();

        GameOverMenu.SetActive(false);

        RefreshStats();

        foreach (UpgradeData upgrade in GameManager.Instance.Data.Upgrades)
        RefreshUpgrade(upgrade);

        RefreshItem(GameManager.Instance.Data.CurrentItem);


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
        else if (FindAnyObjectByType<MeleeController>() != null)
        {
            MeleeController meleeController = FindAnyObjectByType<MeleeController>();
            Ammo.text = "";
        }
    }
    void ApplySettings()
    {
        var data = SaveManager.Instance.Data;

        healthUI.SetActive(data.showHealth);
        statsUI.SetActive(data.showStats);
        upgradesUI.SetActive(data.showUpgrades);

    }
    private void RefreshStats()
    {
        var data = GameManager.Instance.Data;
        Money.text = FormatUI(data.Money);
        EnemiesKilled.text = FormatUI(data.EnemiesKilled);
        RunTime.text = FormatTime(data.RunTime);
        FinalScore.text = FormatUI(data.OverallScore);

        HealthBar.value = data.PlayerHealth;
        HealthText.text = $"{data.PlayerHealth} %";
        ShieldBar.value = data.PlayerShild;
        ShieldText.text = $"{data.PlayerShild} %";
    }
    private void RefreshItem(ItemData item)
    {
        if (Item == null) return;

        Image icon = Item.GetComponent<Image>();
        if (icon == null) return;

        if (item != null && item.itemIcon != null)
            icon.sprite = item.itemIcon;
        else
            icon.sprite = DefaultItemSprite;
    }
    private void RefreshUpgrade(UpgradeData upgrade)
    {
        GameObject iconObj = Instantiate(_upgradeIconPrefab, UpgradeHolder.transform);
        Image iconImage = iconObj.GetComponent<Image>();

        if (iconImage != null)
            iconImage.sprite = upgrade.upgradeIcon;
    }
    private string FormatUI(float amount)
    {
        if (amount >= 1000000f) return $"{amount / 1000000f:0.#}M";
        if (amount >= 1000f) return $"{amount / 1000f:0.#}K";
        return amount.ToString("0");
    }
    private string FormatTime(float time)
    {
        int hours = Mathf.FloorToInt(time / 3600f);
        int minutes = Mathf.FloorToInt((time % 3600f) / 60f); 
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }
     void RefreshGameOver()
    {
        GameOverMenu.SetActive(true);
    }
    // Only for GameOverMenu
    public void Retry()
    {
        GameManager.Instance.StartGame();
    }




    //private void UpdateHealth(float currentHealth, float maxHealth)
    //{
    //}
    //private void UpdateShield(float currentShield, float maxShield)
    //{
    //}
    //private void UpdateStats(string levelName, float time, int enemiesKilled, int money, int finalScore)
    //{

    //}
    //private void UpdateUpgrades(List<string> upgrades)
    //{
    //}
    //private void UpdateItem(string itemName)
    //{
    //}
    //private void UpdateAmmo(int currentAmmo, int magazineSize, bool isReloading)
    //{
    //}
}
