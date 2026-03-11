using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    /// <summary>
    /// TODO
    /// -create a credits menu prefab and use that instead of the current one, which is a placeholder
    /// </summary>
    [SerializeField] private GameObject MainMenu;
    [SerializeField] private GameObject SettingsMenu;
    [SerializeField] private GameObject CreditsMenu;
    [SerializeField] private GameObject StatsMenu;
    [SerializeField] private SettingsController settingsController; 

    [SerializeField] private GameObject CreditsText;
    [SerializeField] private float scrollSpeed = 20f;

    [SerializeField] private TextMeshProUGUI MoneyStats;
    [SerializeField] private TextMeshProUGUI TimeStats;
    [SerializeField] private TextMeshProUGUI EnemiesKilledStats;
    [SerializeField] private TextMeshProUGUI OverallStats;

    public CanvasGroup backgroundGroup;

    private bool fadeout = false;
    private bool fadein = false;
    private Vector3 _creditsStartPosition;
    private bool _statsVisible = false;

    private void Start()
    {
        _creditsStartPosition = CreditsText.transform.localPosition;
    }

    private void Update()
    {
        if (fadeout)
        {

            backgroundGroup.alpha -= Time.deltaTime * 1f;

            if (backgroundGroup.alpha <= 0)
            {
                fadeout = false;
                backgroundGroup.alpha = 0;
                CreditsMenu.SetActive(true);
                backgroundGroup.gameObject.SetActive(false);
            }
        }
        else if (fadein)
        {
            CreditsMenu.SetActive(false);
            backgroundGroup.gameObject.SetActive(true);
            backgroundGroup.alpha += Time.deltaTime * 1f;
            if (backgroundGroup.alpha >= 1)
            {
                fadein = false;
                backgroundGroup.alpha = 1;
            }
        }
        else if (CreditsMenu.activeSelf)
        {
            ScrollCredits();
        }

    }
    public void StartGame()
    {
        HideStats();
        GameManager.Instance.StartGame();
    }
    public void OpenSettings()
    {
        HideStats();
        settingsController.OpenMenu(SettingsMenu); 
    }
    public void QuitGame()
    {
        GameManager.Instance.QuitGame();
    }
    public void GitHubButton()
    {
        Application.OpenURL("https://github.com/BenZimmermann");
    }
    public void Stats()
    {
        _statsVisible = !_statsVisible;
        //load the stats from the SaveManager and show them in the StatsMenu

        var data = SaveManager.Instance.Data;
        MoneyStats.text = FormatTxt(data.Money);
        TimeStats.text = FormatTime(data.Time);

        EnemiesKilledStats.text = FormatTxt(data.EnemiesKilled);
        OverallStats.text = FormatTxt(data.FinalScore);

        StatsMenu.SetActive(_statsVisible);
    }
    private void HideStats()
    {
        _statsVisible = false;
        StatsMenu.SetActive(false);
    }
    public void CreditsBack()
    {
        fadein = true;
    }
    public void Credits()
    {
        HideStats();
        // Credits-Text auf Startposition zurücksetzen
        CreditsText.transform.localPosition = _creditsStartPosition;
        fadeout = true;
        ScrollCredits();

    }
    private void ScrollCredits()
    {

        CreditsText.transform.Translate(Vector3.up * scrollSpeed * Time.deltaTime);
    }
    #region Formatting
    private string FormatTxt(float amount)
    {
        if (amount >= 1000000f) return $"{amount / 1000000f:0.#}M";
        if (amount >= 1000f) return $"{amount / 1000f:0.#}K";
        return amount.ToString("0");
    }
    private string FormatTime(float time)
    {
        int hours = Mathf.FloorToInt(time / 3600f);
        int minutes = Mathf.FloorToInt((time % 3600f) / 60f); // Nur verbleibende Minuten
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }
    #endregion
}
