using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RestManager : MonoBehaviour
{
    public Image image;
    public Sprite partySprite;
    public Sprite soloSprite;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.GetInt("HasCompanion") == 1)
        {
            image.sprite = partySprite;
        }
        else
        {
            image.enabled = false;
        }

        int playerHP = PlayerPrefs.GetInt("PlayerHealth");
        playerHP += 20;
        PlayerPrefs.SetInt("PlayerHealth", playerHP);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
