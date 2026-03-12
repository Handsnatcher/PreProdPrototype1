using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class EnemyBehaviour : MonoBehaviour
{
    public Player player;

    [Header("Attributes")]
    public int enemyMaxHealth = 60;         //max health
    public int enemyMaxDefense = 20;
    public int enemyAttackCardDamage = 30;   //damage value when attacking
    public int enemyDefenseCardValue = 5;   //how many hit points can it defend itself from

    public enum enemyDifficulty { easy, medium, difficult }; //affects "smartness" of AI

    public int enemyCurrentHealth;
    public int enemyCurrentDefense; //how many hit points to negate

    [Header("UI")]
    public Slider enemyHealthSlider;
    public GameObject defenseSlider;
    public Slider enemyDefenseSlider;

    [Header("Audio")]
    private AudioSource source;
    public AudioClip attackSoundEffect;
    public AudioClip defendSoundEffect;

    [Header("Design")]
    //design attributes
    [SerializeField] private Mesh enemyMesh;        //model
    [SerializeField] private Animation enemyIdle;   //idle animation
    [SerializeField] private Mesh enemyAttack;      //attack card to play
    [SerializeField] private Mesh enemyDefense;     //defense card to play
    [SerializeField] private Mesh enemySkill;       //skill card to play?

    private HitFlash hitFlash;

    //ideas for future: enemy states? can change behaviour depending on health
    //e.g. if health <= 50% of max health, attack; else if health >= 50% of max health, have a 3/4 chance to defend, otherwise attack

    void Start()
    {
        enemyCurrentHealth = enemyMaxHealth;
        UpdateEnemyHealthSlider(enemyCurrentHealth, enemyMaxHealth);
        defenseSlider.SetActive(false);
        source = GetComponent<AudioSource>();
        hitFlash = GetComponent<HitFlash>();
    }


    //FOR PROTOTYPE PURPOSES, SIMPLE ENEMY TURN
    public void EnemyTurn()
    {
        //random chance to attack vs defend
        if (Random.value < 0.4f && enemyCurrentDefense <= enemyMaxDefense)
        {
            Debug.Log("Enemy Defended!");
            EnemyDefense();
            TurnManager.Instance.UpdateMoveText(Color.blue, "Defended!");
        }
        else
        {
            EnemyAttack();
        }

    }

    private void EnemyAttack()
    {
        Debug.Log("Enemy Attacked!");
        if (player)
        {
            //NOTE: damage value can be changed depending on difficulty later (for now default is 10)
            player.TakeDamage(enemyAttackCardDamage);

            //show damage UI
            TurnManager.Instance.UpdateMoveText(Color.red, "Attacked!");

            //play sound
            if (source != null && attackSoundEffect != null)
            {
                source.PlayOneShot(attackSoundEffect, 0.5f);
            }
        }
    }

    private void EnemyDefense()
    {
        enemyCurrentDefense += enemyDefenseCardValue;
        enemyCurrentDefense = Mathf.Clamp(enemyCurrentDefense, 0, enemyMaxDefense);

        if (enemyCurrentDefense > 0)
        {
            defenseSlider.SetActive(true);
        }

        UpdateEnemyDefenseSlider(enemyCurrentDefense, enemyMaxDefense);

        //play sound
        if (source != null && defendSoundEffect != null)
        {
            source.PlayOneShot(defendSoundEffect, 0.5f);
        }

    }

    //enemy take damage
    public void EnemyTakeDamage(int playerDamage)
    {
        int finalDamage = Mathf.Max(playerDamage - enemyCurrentDefense, 0);   //to make sure defense doesnt heal enemy on accident
        enemyCurrentHealth -= finalDamage;
        enemyCurrentHealth = Mathf.Clamp(enemyCurrentHealth, 0, enemyMaxHealth);

        UpdateEnemyHealthSlider(enemyCurrentHealth, enemyMaxHealth);
        UpdateEnemyDefenseSlider(enemyCurrentDefense, enemyMaxDefense);

        // Hit flash and screen shake when player takes damage
        hitFlash?.Flash();
        ScreenShake.Instance?.Shake(0.3f, 0.12f);

        if (enemyCurrentHealth <= 0)
        {
            EnemyDeath();
        }

        //defense down
        if (enemyCurrentDefense <= 0)
        {
            defenseSlider.SetActive(false);
        }
    }

    public void EnemyDeath()
    {
        Debug.Log("Enemy died");
        //TODO: check if other enemies are in scene, if none then level is won
    }

    public void UpdateEnemyHealthSlider(int enemyCurrentHealth, int enemyMaxHealth)
    {
        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.maxValue = enemyMaxHealth;
            enemyHealthSlider.value = enemyCurrentHealth;
        }
    }

    public void UpdateEnemyDefenseSlider(int enemyCurrentDefense, int enemyMaxDefense)
    {
        if (enemyDefenseSlider != null)
        {
            enemyDefenseSlider.maxValue = enemyMaxDefense;
            enemyDefenseSlider.value = enemyCurrentDefense;
        }
    }

    ///////////////////////////////ANIMATION FUNCTIONS///////////////////////////////

    //play hurt animation


    //play attack animation


    //play defend animation


    //play defeated animation


    ///////////////////////////////SOUND FUNCTIONS//////////////////////////////////

    //play hurt sound


    //play attack sound


    //play defend sound


    //play defeated sound

}
