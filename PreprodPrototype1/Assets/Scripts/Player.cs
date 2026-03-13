using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour
{
    public int playerMaxHealth = 100;
    public int playerCurrentHealth;
    public int playerMaxDefense = 100;
    public int playerCurrentDefense; //how many hit points to negate
    public int playerAttackCardDamage = 20;   //damage value when attacking
    public int playerDefenseCardValue = 20;   //how many hit points can it defend itself from

    private const string PLAYER_HEALTH_KEY = "PlayerHealth";    //for player prefs

    [Header("Debug")]
    public bool debugKey = true;

    [Header("UI")]
    public Slider playerHealthSlider;
    public GameObject defenseSlider;
    public Slider playerDefenseSlider;

    [Header("Audio")]
    private AudioSource source;
    public AudioClip attackSoundEffect;
    public AudioClip defendSoundEffect;

    private DynamicCameraSystem cameraSystem;

    private HitFlash hitFlash;

    private void Start()
    {
        //set player health
        if (PlayerPrefs.HasKey(PLAYER_HEALTH_KEY))
        {
            playerCurrentHealth = PlayerPrefs.GetInt(PLAYER_HEALTH_KEY);
        }
        else
        {
            playerCurrentHealth = playerMaxHealth;
            PlayerPrefs.SetInt(PLAYER_HEALTH_KEY, playerCurrentHealth);
        }

        hitFlash = GetComponent<HitFlash>();

        Camera mainCam = Camera.main;
        if (mainCam != null)
            cameraSystem = mainCam.GetComponent<DynamicCameraSystem>();

        source = GetComponent<AudioSource>();
        if (cameraSystem == null)
            Debug.LogWarning("Player: No camera system.");

        UpdatePlayerHealthSlider(playerCurrentHealth, playerMaxHealth);
        defenseSlider.SetActive(false);
    }

    //DEBUGGING HEALTH
    void Update()
    {
        if (debugKey && Input.GetKeyDown(KeyCode.H))
        {
            Heal(10);
        }

        if (debugKey && Input.GetKeyDown(KeyCode.T))
            cameraSystem?.EnterTargetingMode();

        if (debugKey && Input.GetKeyUp(KeyCode.T))
            cameraSystem?.ExitTargetingMode();
    }

    public void TakeDamage(int enemyDamage = 10)
    {
        //deal damage to player
        if (playerCurrentDefense > 0)
        {
            playerCurrentDefense -= enemyDamage;
        }
        else if (playerCurrentDefense <= 0)
        {
            defenseSlider.SetActive(false);
            playerCurrentHealth -= enemyDamage;
        }

        hitFlash?.Flash();

        playerCurrentHealth = Mathf.Clamp(playerCurrentHealth, 0, playerMaxHealth);
        PlayerPrefs.SetInt(PLAYER_HEALTH_KEY, playerCurrentHealth);

        UpdatePlayerHealthSlider(playerCurrentHealth, playerMaxHealth);
        UpdatePlayerDefenseSlider(playerCurrentDefense, playerMaxDefense);

        if (playerCurrentHealth <= 0)
        {
            TurnManager.Instance.SetGameOver();
        }
    }

    //call when defense card is played
    public void PlayerDefense(int value)
    {

        playerCurrentDefense += value;
        Debug.Log("Player defense" + playerCurrentDefense);
        // playerCurrentDefense = Mathf.Clamp(playerCurrentDefense, 0, playerMaxDefense);
        Debug.Log("Player defense after clamp" + playerCurrentDefense);

        TurnManager.Instance.UpdateMoveText(Color.blue, "Defended!" + playerCurrentDefense);

        if (playerCurrentDefense > 0)
        {
            defenseSlider.SetActive(true);
        }

        UpdatePlayerDefenseSlider(playerCurrentDefense, playerMaxDefense);

        //play sound
        if (source != null && defendSoundEffect != null)
        {
            source.PlayOneShot(defendSoundEffect, 0.5f);
        }

    }

    //call when attack card is played
    [System.Obsolete]
    public void PlayerAttack()
    {
        EnemyBehaviour[] enemies = FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None);

        if (enemies.Length == 0)
        {
            Debug.LogWarning("Player: No enemies found to attack.");
            return;
        }

        // Enter targeting mode so player can pick a target
        cameraSystem?.EnterTargetingMode();

        // For now pick a random living enemy - swap this out when card targeting UI is ready
        EnemyBehaviour target = null;
        foreach (var e in enemies)
        {
            if (e.enemyCurrentHealth > 0)
            {
                target = e;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning("Player: All enemies are dead.");
            cameraSystem?.ExitTargetingMode();
            return;
        }

        TurnManager.Instance.UpdateMoveText(Color.red, "Attacked!");
        target.EnemyTakeDamage(playerAttackCardDamage);

        // Return camera to dynamic shots after attack resolves
        cameraSystem?.ExitTargetingMode();

        //play sound
        if (source != null && attackSoundEffect != null)
        {
            source.PlayOneShot(attackSoundEffect, 0.5f);
        }
    }

    public void UpdatePlayerHealthSlider(int playerCurrentHealth, int playerMaxHealth)
    {
        if (playerHealthSlider != null)
        {
            playerHealthSlider.maxValue = playerMaxHealth;
            playerHealthSlider.value = playerCurrentHealth;
        }
    }
    public void UpdatePlayerDefenseSlider(int playerCurrentDefense, int playerMaxDefense)
    {
        if (playerDefenseSlider != null)
        {
            playerDefenseSlider.maxValue = playerMaxDefense;
            playerDefenseSlider.value = playerCurrentDefense;
        }
    }

    public void Heal(int  healAmount)
    {
        //NOTE: call during rest sites/powerups/after dialogue/whenever needed
        playerCurrentHealth += healAmount;

        playerCurrentHealth = Mathf.Clamp(playerCurrentHealth, 0, playerMaxHealth);

        PlayerPrefs.SetInt(PLAYER_HEALTH_KEY, playerCurrentHealth);

        UpdatePlayerHealthSlider(playerCurrentHealth, playerMaxHealth);
    }

}
