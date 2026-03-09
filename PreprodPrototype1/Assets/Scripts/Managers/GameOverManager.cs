using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public string combatSceneName = "JandreTest";
    public string mainMenuSceneName = "MainMenuScene";

    public void TryAgain()
    {
        PlayerPrefs.DeleteKey("PlayerHealth"); // reset health so player starts fresh
        SceneManager.LoadScene(combatSceneName);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}