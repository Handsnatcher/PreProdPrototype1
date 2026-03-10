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
    public int playerAttackCardDamage = 30;   //damage value when attacking
    public int playerDefenseCardValue = 10;   //how many hit points can it defend itself from

    private const string PLAYER_HEALTH_KEY = "PlayerHealth";    //for player prefs

    [Header("Debug")]
    public bool debugKey = true;

    [Header("UI")]
    public Slider playerHealthSlider;
    public GameObject defenseSlider;
    public Slider playerDefenseSlider;

    private DynamicCameraSystem cameraSystem;

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

        cameraSystem = Camera.main.GetComponent<DynamicCameraSystem>();
        if (cameraSystem == null)
            Debug.LogWarning("Player: No DynamicCameraSystem found on Main Camera.");

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
        playerCurrentHealth -= enemyDamage;

        playerCurrentHealth = Mathf.Clamp(playerCurrentHealth, 0, playerMaxHealth);
        PlayerPrefs.SetInt(PLAYER_HEALTH_KEY, playerCurrentHealth);

        UpdatePlayerHealthSlider(playerCurrentHealth, playerMaxHealth);
        UpdatePlayerDefenseSlider(playerCurrentDefense, playerMaxDefense);

        if (playerCurrentHealth <= 0)
        {
            TurnManager.Instance.SetGameOver();
        }
        //defense down
        if (playerCurrentDefense <= 0)
        {
            defenseSlider.SetActive(false);
        }
    }

    //call when defense card is played
    public void PlayerDefense()
    {

        playerCurrentDefense += playerDefenseCardValue; 
        playerCurrentDefense = Mathf.Clamp(playerCurrentDefense, 0, playerMaxDefense);

        TurnManager.Instance.UpdateMoveText(Color.blue, "Defended!" + playerCurrentDefense);

        if (playerCurrentDefense > 0)
        {
            defenseSlider.SetActive(true);
        }

        UpdatePlayerDefenseSlider(playerCurrentDefense, playerMaxDefense);

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
