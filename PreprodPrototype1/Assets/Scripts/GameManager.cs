using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager gameManagerInstance;

    public int levelsCompleted = 0;         //levels completed


    /*
    * sets up game manager instance
    */
    private void Awake()
    {
        //make sure only one game manager exists
        if (gameManagerInstance == null)
        {
            gameManagerInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        //load saved progress
        levelsCompleted = PlayerPrefs.GetInt("LevelsCompleted", 0);
    }

    /*
    * sets level as completed
    */
    public void MarkLevelCompleted(int levelNumber)
    {
        if (levelNumber > levelsCompleted)
        {
            levelsCompleted = levelNumber;
            PlayerPrefs.SetInt("LevelsCompleted", levelsCompleted);
            PlayerPrefs.Save();
        }
    }
}
