using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "Game";

    [Header("HowTo")]
    public GameObject howToPlayPanel;  // Panel (окно HowToPlay)

    [Header("Main Menu Objects to Hide")]
    public GameObject menuFrame;       // MenuFrame
    public GameObject menuButtons;     // MenuButtons (если используешь)
    public GameObject startButton;     // StartButton
    public GameObject howToPlayButton; // HowToPlayButton
    public GameObject quitButton;      // QuitButton

    void Start()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        ShowMainMenu(true);
    }

    void ShowMainMenu(bool show)
    {
        if (menuFrame) menuFrame.SetActive(show);
        if (menuButtons) menuButtons.SetActive(show);
        if (startButton) startButton.SetActive(show);
        if (howToPlayButton) howToPlayButton.SetActive(show);
        if (quitButton) quitButton.SetActive(show);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(true);
        ShowMainMenu(false);
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        ShowMainMenu(true);
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");
        Application.Quit();
    }
}