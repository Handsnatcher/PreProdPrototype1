using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int playerMaxHealth = 100;
    public int playerCurrentHealth;
    public int playerCurrentDefense; //how many hit points to negate
    public int playerAttackCardDamage = 10;   //damage value when attacking
    public int playerDefenseCardValue = 10;   //how many hit points can it defend itself from

    private const string PLAYER_HEALTH_KEY = "PlayerHealth";    //for player prefs

    [Header("Debug")]
    public bool debugKey = true;

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

        UpdateHealthUI();
    }

    //DEBUGGING HEALTH
    void Update()
    {
        if (debugKey && Input.GetKeyDown(KeyCode.H))
        {
            Heal(10);
        }
    }

    public void TakeDamage(int enemyDamage = 10)
    {
        //deal damage to player
        playerCurrentHealth -= enemyDamage;

        playerCurrentHealth = Mathf.Clamp(playerCurrentHealth, 0, playerMaxHealth);
        PlayerPrefs.SetInt(PLAYER_HEALTH_KEY, playerCurrentHealth);

        UpdateHealthUI();

        if (playerCurrentHealth <= 0)
        {
            TurnManager.Instance.SetGameOver();
        }
    }

    //TODO: call when defense card is played
    public void PlayerDefense()
    {
        TurnManager.Instance.UpdateMoveText(Color.blue, "Defended!");
        playerCurrentDefense = playerCurrentDefense + playerDefenseCardValue;
    }

    //TODO: call when attack card is played
    [System.Obsolete]
    public void PlayerAttack()
    {
        //TODO: select enemy, for now just find random enemy and attack them
        EnemyBehaviour[] enemies = FindObjectsOfType<EnemyBehaviour>();

        if (enemies.Length > 0)
        {
            EnemyBehaviour target = enemies[Random.Range(0, enemies.Length)];

            //make sure enemy isnt dead
            if (target.enemyCurrentHealth > 0)
            {
                TurnManager.Instance.UpdateMoveText(Color.red, "Attacked!");
                target.EnemyTakeDamage(playerAttackCardDamage);
            }
        }

    }

    private void UpdateHealthUI()
    {
        TurnManager.Instance.UpdatePlayerHealthSlider(playerCurrentHealth, playerMaxHealth);
    }

    public void Heal(int  healAmount)
    {
        //NOTE: call during rest sites/powerups/after dialogue/whenever needed
        playerCurrentHealth += healAmount;

        playerCurrentHealth = Mathf.Clamp(playerCurrentHealth, 0, playerMaxHealth);

        PlayerPrefs.SetInt(PLAYER_HEALTH_KEY, playerCurrentHealth);

        UpdateHealthUI();
    }

}
