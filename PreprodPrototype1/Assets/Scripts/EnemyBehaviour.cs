using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{

    //enemy attributes
    GameObject[] enemies; 
    public int maxHealth = 100;      //max health
    public int attackDamage = 10;   //damage value when attacking
    public int defenseValue = 10;   //how many hit points can it defend itself from
    public enum difficulty { easy, medium, difficult }; //affects "smartness" of AI

    private int health;
    private float timer;
    private Coroutine enemyThinkingCoroutine;

    //design attributes
    [SerializeField] private Mesh enemyMesh;        //model
    [SerializeField] private Animation enemyIdle;   //idle animation
    [SerializeField] private Mesh enemyAttack;      //attack card to play
    [SerializeField] private Mesh enemyDefense;     //defense card to play
    [SerializeField] private Mesh enemySkill;       //skill card to play?

    //ideas for future: enemy states? can change behaviour depending on health
    //e.g. if health <= 50% of max health, attack; else if health >= 50% of max health, have a 3/4 chance to defend, otherwise attack

    void Start()
    {
        health = maxHealth;
    }

    public void EnemyStart()
    {
        if (health > 0)
        {
            enemyThinkingCoroutine = StartCoroutine(EnemyThinking());
        }
    }

    private IEnumerator EnemyThinking()
    {
        Debug.Log("Enemy thinking......");

        yield return new WaitForSeconds(2.0f); //how long the enemy "thinks" for

        EnemyTurn();
    }

    //FOR DEBUG PURPOSES, SIMPLE ENEMY TURN
    private void EnemyTurn()
    {
        //random chance to attack vs defend
        if (Random.value < 0.5f)
        {
            Debug.Log("Enemy Attacked!");
        }
        else
        {
            Debug.Log("Enemy Defended!");
        }

        TurnManager.Instance.EndEnemyTurn();
    }



    //enemy defeated


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
