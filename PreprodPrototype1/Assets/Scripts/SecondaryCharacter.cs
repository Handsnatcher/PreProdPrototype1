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
    public float shakeSpeed = 2.0f;
    public float shakeAmount = 1.0f;
    float timer = 0;

    void Start()
    {
        Setup();
    }

    void Update()
    {
        if (shouldShake)
        {
            ObjShake();
        }
    }

    public void Regeneration()
    {
        StartCoroutine(WaitFor(0.7f, HealEffect));
    }

    private void HealEffect()
    {
        Debug.Log("Healed");
        player.Heal(regenAmount);
        TurnManager.Instance.UpdateMoveText(Color.green, "+ " + regenAmount.ToString());
        shouldShake = true;
    }

    private void Setup()
    {
        player = FindFirstObjectByType<Player>();
        TurnManager.Instance.OnPlayerTurnStart.AddListener(Regeneration);
        if(PlayerPrefs.GetInt("HasCompanion") == 0)
        {
            gameObject.SetActive(false);
        }
    }

    private void ObjShake()
    {
        Vector3 pos = transform.position;
        pos.x += Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
        transform.position = pos;

        // set a timer to stop shaking
        if (timer < 1.0f)
        {
            timer += Time.deltaTime;
        }
        else
        {
            shouldShake = false;
            timer = 0;
        }
    }

    IEnumerator WaitFor(float sec, System.Action action)
    {
        Debug.Log("waiting...");
        yield return new WaitForSeconds(sec);
        Debug.Log("start action");
        action?.Invoke();
    }
}
