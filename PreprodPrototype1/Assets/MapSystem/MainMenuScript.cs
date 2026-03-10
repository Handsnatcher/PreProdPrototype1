using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        PlayerPrefs.SetInt("MapLevel", 0);
        PlayerPrefs.SetInt("PlayerHealth", 100);
        PlayerPrefs.SetInt("HasCompanion", 0);
        SceneManager.LoadScene("MapScene");
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene("MapScene");
    }
}
