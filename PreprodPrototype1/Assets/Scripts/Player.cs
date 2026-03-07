using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int playerMaxHealth = 100;
    public int playerCurrentHealth;

    private void Start()
    {
        playerCurrentHealth = playerMaxHealth;
    }

    public void TakeDamage(int enemyDamage = 10)
    {
        playerCurrentHealth = playerCurrentHealth - enemyDamage;

        if (playerCurrentHealth <= 0)
        {
            TurnManager.Instance.SetGameOver();
        }
    }
}
