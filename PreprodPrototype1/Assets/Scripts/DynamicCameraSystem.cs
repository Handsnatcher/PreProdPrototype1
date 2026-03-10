using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CameraShot
{
    [Tooltip("Offset from the target's position")]
    public Vector3 positionOffset = new Vector3(0f, 1.5f, -3f);

    [Tooltip("Where the camera looks")]
    public Vector3 lookOffset = new Vector3(0f, 1f, 0f);

    [Tooltip("Direction the camera slowly drifts during the hold")]
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
            positionOffset = new Vector3(-4f, 2.5f, -6f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(1f, 0f, 0f),
            dollySpeed     = 0.4f,
            holdDuration   = 3f,
            blendDuration  = 0.8f
        },
        new CameraShot
        {
            positionOffset = new Vector3(0f, 4f, -7f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(-0.5f, 0f, 0.2f),
            dollySpeed     = 0.3f,
            holdDuration   = 3f,
            blendDuration  = 1f
        },
        new CameraShot
        {
            positionOffset = new Vector3(4f, 2f, -6f),
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
            positionOffset = new Vector3(3f, 3f, -6f),
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

    [Header("Targeting Shot")]
    [Tooltip("Offset from the player's position for the over-the-shoulder shot")]
    public Vector3 targetingPositionOffset = new Vector3(1.2f, 1.6f, -1.5f);
    [Tooltip("How long to blend into and out of the targeting shot")]
    public float targetingBlendDuration = 0.4f;

    private Transform activeTarget;
    private List<CameraShot> activeShots;
    private int currentShotIndex = 0;
    private Vector3 dollyAccumulator = Vector3.zero;
    private bool isTargeting = false;

    private Coroutine shotCycleCoroutine;
    private Coroutine blendCoroutine;
    private Coroutine targetingCoroutine;

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.AddListener(SwitchToPlayerCamera);
            TurnManager.Instance.OnPlayerTurnEnd.AddListener(SwitchToEnemyCamera);
        }
        else
        {
            Debug.LogWarning("DynamicCameraSystem: No TurnManager found.");
        }

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
        if (isTargeting) return;

        if (playerTarget == null)
        {
            Debug.LogWarning("DynamicCameraSystem: playerTarget is not assigned.");
            return;
        }

        activeTarget = playerTarget;
        activeShots = playerShots;
        BeginShotCycle();
    }

    public void SwitchToEnemyCamera()
    {
        if (isTargeting) return;

        if (enemyTarget == null)
        {
            Debug.LogWarning("DynamicCameraSystem: enemyTarget is not assigned.");
            return;
        }

        activeTarget = enemyTarget;
        activeShots = enemyShots;
        BeginShotCycle();
    }

    // TARGETING

    /// <summary>
    /// Call this from the card system when the player plays an attack card.
    /// Stops the dynamic camera and moves to an over-the-shoulder targeting shot.
    /// </summary>
    public void EnterTargetingMode()
    {
        if (isTargeting) return;
        if (playerTarget == null || enemyTarget == null)
        {
            Debug.LogWarning("DynamicCameraSystem: playerTarget or enemyTarget is not assigned.");
            return;
        }

        isTargeting = true;

        // Stop the dynamic shot cycle
        if (shotCycleCoroutine != null) StopCoroutine(shotCycleCoroutine);
        if (blendCoroutine != null) StopCoroutine(blendCoroutine);

        targetingCoroutine = StartCoroutine(BlendToTargetingShot());
        Debug.Log("DynamicCamera: Entered targeting mode.");
    }

    /// <summary>
    /// Call this from the card system after the player has chosen a target and the attack is resolved.
    /// Returns the camera to the dynamic shot cycle.
    /// </summary>
    public void ExitTargetingMode()
    {
        if (!isTargeting) return;

        isTargeting = false;

        if (targetingCoroutine != null) StopCoroutine(targetingCoroutine);

        // Resume from the player dynamic shots
        activeTarget = playerTarget;
        activeShots = playerShots;
        BeginShotCycle();

        Debug.Log("DynamicCamera: Exited targeting mode.");
    }

    private IEnumerator BlendToTargetingShot()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float elapsed = 0f;
        while (elapsed < targetingBlendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / targetingBlendDuration);

            // Over-the-shoulder: offset from player, looking at enemy
            Vector3 targetPos = playerTarget.position + targetingPositionOffset;
            Vector3 lookDir = (enemyTarget.position + Vector3.up) - targetPos;

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, Quaternion.LookRotation(lookDir), t);

            yield return null;
        }

        // Hold the targeting shot, tracking the enemy while targeting is active
        while (isTargeting)
        {
            Vector3 targetPos = playerTarget.position + targetingPositionOffset;
            Vector3 lookDir = (enemyTarget.position + Vector3.up) - targetPos;

            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation(lookDir);

            yield return null;
        }
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

            // Stop if target was destroyed
            if (activeTarget == null)
            {
                yield break;
            }

            CameraShot shot = activeShots[currentShotIndex];
            dollyAccumulator = Vector3.zero;

            blendCoroutine = StartCoroutine(BlendToShot(shot));
            yield return blendCoroutine;

            float elapsed = 0f;
            while (elapsed < shot.holdDuration)
            {
                // Stop if target was destroyed mid-hold
                if (activeTarget == null) yield break;

                elapsed += Time.deltaTime;
                dollyAccumulator += shot.dollyDirection * shot.dollySpeed * Time.deltaTime;

                transform.position = activeTarget.position + shot.positionOffset + dollyAccumulator;
                transform.LookAt(activeTarget.position + shot.lookOffset);

                yield return null;
            }

            currentShotIndex = (currentShotIndex + 1) % activeShots.Count;
        }
    }

    private IEnumerator BlendToShot(CameraShot shot)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 endPos = activeTarget.position + shot.positionOffset;
        Quaternion endRot = Quaternion.LookRotation((activeTarget.position + shot.lookOffset) - endPos);

        float elapsed = 0f;
        while (elapsed < shot.blendDuration)
        {
            if (activeTarget == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / shot.blendDuration);

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        if (activeTarget == null) yield break;

        transform.position = endPos;
        transform.rotation = endRot;
    }
}