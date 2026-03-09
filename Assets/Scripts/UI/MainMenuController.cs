using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject MainMenu;
    [SerializeField] private GameObject SettingsMenu;
    [SerializeField] private GameObject CreditsMenu;

    [SerializeField] private GameObject CreditsText;
    [SerializeField] private float scrollSpeed = 20f;

    public CanvasGroup backgroundGroup;

    private bool fadeout = false;
    private bool fadein = false;



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
        GameManager.Instance.StartGame();
    }
    public void OpenSettings()
    {
        MainMenu.SetActive(false);
        SettingsMenu.SetActive(true);
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

    }
    public void CreditsBack()
    {
        fadein = true;
    }
    public void Credits()
    {
        fadeout = true;
        ScrollCredits();

    }
    private void ScrollCredits()
    {

        CreditsText.transform.Translate(Vector3.up * scrollSpeed * Time.deltaTime);
    }
}
