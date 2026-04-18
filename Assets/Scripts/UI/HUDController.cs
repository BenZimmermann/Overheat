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
    [SerializeField] private Image _cooldownOverlay;

    [Header("UI Panels")]
    public GameObject healthUI;
    public GameObject statsUI;
    public GameObject upgradesUI;

    [Header("Ammo")]
    [SerializeField] public TextMeshProUGUI Ammo;

    [Header("GameOver")]
    [SerializeField] public GameObject GameOverMenu;
    [SerializeField] public GameObject FinishGameMenu;
    [SerializeField] public TextMeshProUGUI FinishGameTime;

    private float _cooldownTotal;
    private float _cooldownRemaining;
    private ItemData _trackedItem;

    void OnEnable()
    {
        SettingsManager.Instance.OnSettingsChanged += ApplySettings;
        GameManager.Instance.Data.OnDataChanged += RefreshStats;
        GameManager.Instance.Data.OnUpgradeAdded += RefreshUpgrade;
        GameManager.Instance.Data.OnItemChanged += RefreshItem;
        GameManager.Instance.OnGameOver += RefreshGameOver;
        GameManager.Instance.OnFinishGame += RefreshFinishGame;

    }

    void OnDisable()
    {
        SettingsManager.Instance.OnSettingsChanged -= ApplySettings;
        GameManager.Instance.Data.OnDataChanged -= RefreshStats;
        GameManager.Instance.Data.OnUpgradeAdded -= RefreshUpgrade;
        GameManager.Instance.Data.OnItemChanged -= RefreshItem;
        GameManager.Instance.OnGameOver -= RefreshGameOver;
        GameManager.Instance.OnFinishGame -= RefreshFinishGame;
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

        if (_cooldownOverlay != null)
            _cooldownOverlay.fillAmount = 0f;
    }

    private void Update()
    {
        // Ammo
        if (FindAnyObjectByType<ShootController>() != null)
        {
            ShootController shootController = FindAnyObjectByType<ShootController>();
            Ammo.text = $"{shootController.CurrentAmmo} / {shootController.MagazineSize}";
            if (shootController.IsReloading)
                Ammo.text = "Reloading...";
        }
        else if (FindAnyObjectByType<MeleeController>() != null)
        {
            Ammo.text = "";
        }

        // Cooldown tick
        TickCooldown();
    }

    // -------------------------------------------------------------------------
    // Cooldown
    // -------------------------------------------------------------------------

    /// <summary>
    /// Startet den Cooldown-Indicator. Wird vom ItemController aufgerufen.
    /// </summary>
    public void StartCooldown(float duration, ItemData item)
    {
        _cooldownTotal = duration;
        _cooldownRemaining = duration;
        _trackedItem = item;
        UpdateCooldownOverlay(1f); // voll ausgegraut
    }

    private void TickCooldown()
    {
        if (_cooldownRemaining <= 0f) return;

        // Item wurde gewechselt – Cooldown zurücksetzen
        if (GameManager.Instance.Data.CurrentItem != _trackedItem)
        {
            _cooldownRemaining = 0f;
            _trackedItem = null;
            UpdateCooldownOverlay(0f);
            return;
        }

        _cooldownRemaining -= Time.deltaTime;

        if (_cooldownRemaining <= 0f)
        {
            _cooldownRemaining = 0f;
            UpdateCooldownOverlay(0f);
        }
        else
        {
            UpdateCooldownOverlay(_cooldownRemaining / _cooldownTotal);
        }
    }

    private void UpdateCooldownOverlay(float fill)
    {
        if (_cooldownOverlay == null) return;
        _cooldownOverlay.fillAmount = fill;
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

        icon.sprite = (item != null && item.itemIcon != null)
            ? item.itemIcon
            : DefaultItemSprite;

        // Item gewechselt – Cooldown resetten
        if (item != _trackedItem)
        {
            _cooldownRemaining = 0f;
            _trackedItem = null;
            UpdateCooldownOverlay(0f);
        }
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

    void RefreshFinishGame()
    {
        FinishGameMenu.SetActive(true);
        FinishGameTime.text = "Final Time: " + FormatTime(GameManager.Instance.Data.RunTime);
    }
    public void QuitToMainMenu()
    {
        GameManager.Instance.QuitToMainMenu();
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
