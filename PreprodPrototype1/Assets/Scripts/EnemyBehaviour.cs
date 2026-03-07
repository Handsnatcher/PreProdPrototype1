using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    public Player player;

    //enemy attributes
    GameObject[] enemies; 
    public int enemyMaxHealth = 100;         //max health
    public int enemyAttackCardDamage = 10;   //damage value when attacking
    public int enemyDefenseCardValue = 10;   //how many hit points can it defend itself from

    public enum enemyDifficulty { easy, medium, difficult }; //affects "smartness" of AI

    public int enemyCurrentHealth;
    public int enemyCurrentDefense; //how many hit points to negate

    private Coroutine enemyThinkingCoroutine = null;

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
        enemyCurrentHealth = enemyMaxHealth;
    }

    //public void EnemyStart()
    //{
    //    Debug.Log("EnemyStart called");

    //    if (enemyThinkingCoroutine == null)
    //    {
    //        if (enemyCurrentHealth > 0)
    //        {
    //            TurnManager.Instance.NotifyEnemyTurnStarted();
    //            enemyThinkingCoroutine = StartCoroutine(EnemyThinking());
    //        }
    //    }

    //}

    //private IEnumerator EnemyThinking()
    //{
    //    Debug.Log("Enemy thinking......");

    //    yield return new WaitForSeconds(2.0f); //how long the enemy "thinks" for

    //    EnemyTurn();

    //    enemyThinkingCoroutine = null;

    //}

    //FOR DEBUG PURPOSES, SIMPLE ENEMY TURN
    public void EnemyTurn()
    {
        //random chance to attack vs defend
        if (Random.value < 0.5f)
        {
            Debug.Log("Enemy Attacked!");
            if (player)
            {
                //NOTE: damage value can be changed depending on difficulty later (for now default is 10)
                player.TakeDamage();

                //show damage UI
            }
        }
        else
        {
            Debug.Log("Enemy Defended!");
            EnemyDefense();
        }

        TurnManager.Instance.EndEnemyTurn();
    }

    private void EnemyDefense()
    {
        enemyCurrentDefense = enemyCurrentDefense + enemyDefenseCardValue;
    }


    //enemy take damage (CALL WHEN PLAY CARD)
    public void EnemyTakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - enemyCurrentDefense, 0);   //to make sure defense doesnt heal enemy on accident
        enemyCurrentHealth -= finalDamage;
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
