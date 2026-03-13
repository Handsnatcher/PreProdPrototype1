using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public string combatSceneName = "JandreTest";
    public string mainMenuSceneName = "MainMenuScene";

    public void TryAgain()
    {
        CleanupDontDestroyObjects();
        PlayerPrefs.DeleteKey("PlayerHealth"); // reset health so player starts fresh
        SceneManager.LoadScene(combatSceneName);
    }

    public void MainMenu()
    {
        CleanupDontDestroyObjects();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void CleanupDontDestroyObjects()
    {
        GlassShatterEffect shatter = FindAnyObjectByType<GlassShatterEffect>();
        if (shatter != null)
        {
            if (shatter.targetCamera != null)
                Destroy(shatter.targetCamera.gameObject);
            Destroy(shatter.gameObject);
        }

        GameObject shardRoot = GameObject.Find("ShardRoot");
        if (shardRoot != null)
            Destroy(shardRoot);
    }

}