using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CameraShot
{
    [Tooltip("Offset from the target's position")]
    public Vector3 positionOffset = new Vector3(0f, 1.5f, -3f);

    [Tooltip("Where the camera looks. Leave as (0,0,0) to auto-look at target")]
    public Vector3 lookOffset = new Vector3(0f, 1f, 0f);

    [Tooltip("Optional: move the camera along this direction over time (dolly)")]
    public Vector3 dollyDirection = new Vector3(1f, 0f, 0f);

    [Tooltip("Speed of the dolly movement")]
    public float dollySpeed = 0.5f;

    [Tooltip("How long to hold this shot before switching to the next")]
    public float holdDuration = 3f;

    [Tooltip("How long to blend from the previous shot to this one")]
    public float blendDuration = 0.8f;
}

public class DynamicCameraSystem : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTarget;
    public Transform enemyTarget;

    [Header("Player Shots")]
    public List<CameraShot> playerShots = new List<CameraShot>()
    {
        new CameraShot
        {
            positionOffset = new Vector3(-2f, 1.5f, -3f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(1f, 0f, 0f),
            dollySpeed     = 0.4f,
            holdDuration   = 3f,
            blendDuration  = 0.8f
        },
        new CameraShot
        {
            positionOffset = new Vector3(0f, 2.5f, -4f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(-0.5f, 0f, 0.2f),
            dollySpeed     = 0.3f,
            holdDuration   = 3f,
            blendDuration  = 1f
        },
        new CameraShot
        {
            positionOffset = new Vector3(2f, 1f, -2f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(-1f, 0.1f, 0f),
            dollySpeed     = 0.5f,
            holdDuration   = 3f,
            blendDuration  = 0.7f
        }
    };

    [Header("Enemy Shots")]
    public List<CameraShot> enemyShots = new List<CameraShot>()
    {
        new CameraShot
        {
            positionOffset = new Vector3(2f, 1.5f, -3f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(-1f, 0f, 0f),
            dollySpeed     = 0.4f,
            holdDuration   = 3f,
            blendDuration  = 0.8f
        },
        new CameraShot
        {
            positionOffset = new Vector3(0f, 3f, -5f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(0.3f, -0.1f, 0f),
            dollySpeed     = 0.3f,
            holdDuration   = 3f,
            blendDuration  = 1.2f
        }
    };

    // -- Private state --
    private Transform activeTarget;
    private List<CameraShot> activeShots;
    private int currentShotIndex = 0;
    private Vector3 dollyAccumulator = Vector3.zero;

    private Coroutine shotCycleCoroutine;
    private Coroutine blendCoroutine;

    // SETUP

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.AddListener(SwitchToPlayerCamera);
            TurnManager.Instance.OnPlayerTurnEnd.AddListener(SwitchToEnemyCamera);
        }
        else
        {
            Debug.LogWarning("DynamicCameraSystem: No TurnManager found. Camera won't switch automatically.");
        }

        // Start on player
        if (playerTarget != null)
            SwitchToPlayerCamera();
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.RemoveListener(SwitchToPlayerCamera);
            TurnManager.Instance.OnPlayerTurnEnd.RemoveListener(SwitchToEnemyCamera);
        }
    }

    // TURN SWITCHES

    public void SwitchToPlayerCamera()
    {
        if (playerTarget == null)
        {
            Debug.LogWarning("DynamicCameraSystem: playerTarget is not assigned.");
            return;
        }

        activeTarget = playerTarget;
        activeShots = playerShots;

        BeginShotCycle();
        Debug.Log("DynamicCamera: Switched to player shots.");
    }

    public void SwitchToEnemyCamera()
    {
        if (enemyTarget == null)
        {
            Debug.LogWarning("DynamicCameraSystem: enemyTarget is not assigned.");
            return;
        }

        activeTarget = enemyTarget;
        activeShots = enemyShots;

        BeginShotCycle();
        Debug.Log("DynamicCamera: Switched to enemy shots.");
    }

    // SHOT CYCLING

    private void BeginShotCycle()
    {
        if (shotCycleCoroutine != null) StopCoroutine(shotCycleCoroutine);
        if (blendCoroutine != null) StopCoroutine(blendCoroutine);

        currentShotIndex = 0;
        dollyAccumulator = Vector3.zero;
        shotCycleCoroutine = StartCoroutine(ShotCycleRoutine());
    }

    private IEnumerator ShotCycleRoutine()
    {
        while (true)
        {
            if (activeShots == null || activeShots.Count == 0)
            {
                yield return null;
                continue;
            }

            CameraShot shot = activeShots[currentShotIndex];
            dollyAccumulator = Vector3.zero;

            // Blend into this shot
            blendCoroutine = StartCoroutine(BlendToShot(shot));
            yield return blendCoroutine;

            // Hold and dolly
            float elapsed = 0f;
            while (elapsed < shot.holdDuration)
            {
                elapsed += Time.deltaTime;
                dollyAccumulator += shot.dollyDirection * shot.dollySpeed * Time.deltaTime;

                Vector3 targetPos = activeTarget.position
                                  + shot.positionOffset
                                  + dollyAccumulator;

                transform.position = targetPos;
                transform.LookAt(activeTarget.position + shot.lookOffset);

                yield return null;
            }

            // Advance to next shot
            currentShotIndex = (currentShotIndex + 1) % activeShots.Count;
        }
    }

    private IEnumerator BlendToShot(CameraShot shot)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = activeTarget.position + shot.positionOffset;
        Quaternion endRot = Quaternion.LookRotation(
            (activeTarget.position + shot.lookOffset) - endPos
        );

        float elapsed = 0f;

        while (elapsed < shot.blendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / shot.blendDuration);

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
    }
}