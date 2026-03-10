using NUnit.Framework.Internal.Commands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondaryCharacter : MonoBehaviour
{
    public enum secondaryCharacter { Researcher, Devotee, Mercenary, Knight};
    private Player player;
    public int regenAmount = 2;

    //effect
    private bool shouldShake;
    private float shakeSpeed = 2.0f;
    private float shakeAmount = 1.5f;

    void Start()
    {
        SetPlayer();
    }

    void Update()
    {
        if (shouldShake)
        {
            ObjShake();
        }
    }

    void Regeneration()
    {
        if (player == null)
        {
            SetPlayer();
        }

        player.Heal(regenAmount);
        TurnManager.Instance.UpdateMoveText(Color.green, "+ " + regenAmount.ToString());
    }

    void SetPlayer()
    {
        player = FindFirstObjectByType<Player>();
    }

    void ObjShake()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
        transform.position = pos;

        // set a timer to stop shaking
        float timer = Time.deltaTime;
        while (timer < 1)
        {
            timer += Time.deltaTime;
        }
        shouldShake = false;
    }
}
