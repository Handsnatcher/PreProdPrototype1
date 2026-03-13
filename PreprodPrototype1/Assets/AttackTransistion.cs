using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTransistion : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayAttack()
    {
        animator.SetTrigger("Attack");
    }
}
