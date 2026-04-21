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

    #region Cooldown Indicator
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

        // reset item cooldown, wenn der Spieler das Item wechselt oder es ablegt
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
    //updates the cooldown overlay fill amount based on the remaining cooldown time, where 1 means fully on cooldown (overlay fully visible) and 0 means ready to use (overlay hidden).
    private void UpdateCooldownOverlay(float fill)
    {
        if (_cooldownOverlay == null) return;
        _cooldownOverlay.fillAmount = fill;
    }
    #endregion
    void ApplySettings()
    {
        var data = SaveManager.Instance.Data;

        healthUI.SetActive(data.showHealth);
        statsUI.SetActive(data.showStats);
        upgradesUI.SetActive(data.showUpgrades);

    }
    //refreshes the stats in the hud
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
    //refreshes the item icon in the hud
    private void RefreshItem(ItemData item)
    {
        if (Item == null) return;

        Image icon = Item.GetComponent<Image>();
        if (icon == null) return;

        icon.sprite = (item != null && item.itemIcon != null)
            ? item.itemIcon
            : DefaultItemSprite;

        // if the item has changed and is not the tracked item, reset cooldown
        if (item != _trackedItem)
        {
            _cooldownRemaining = 0f;
            _trackedItem = null;
            UpdateCooldownOverlay(0f);
        }
    }
    //refreshes the upgrade icons in the hud, should be called for each upgrade in the player's data when they are added or changed
    private void RefreshUpgrade(UpgradeData upgrade)
    {
        GameObject iconObj = Instantiate(_upgradeIconPrefab, UpgradeHolder.transform);
        Image iconImage = iconObj.GetComponent<Image>();

        if (iconImage != null)
            iconImage.sprite = upgrade.upgradeIcon;
    }
    #region forrmating
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
    #endregion
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

}
