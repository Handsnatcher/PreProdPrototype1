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

    [Header("Framing")]
    [Tooltip("How strongly the camera nudges to keep both characters in frame")]
    [Range(0f, 1f)]
    public float framingStrength = 0.3f;

    [Header("Player Shots")]
    public List<CameraShot> playerShots = new List<CameraShot>()
    {
        new CameraShot
        {
            positionOffset = new Vector3(-1.5f, 2f, -6f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(0.3f, 0f, 0f),
            dollySpeed     = 0.15f,
            holdDuration   = 5f,
            blendDuration  = 1.5f
        },
        new CameraShot
        {
            positionOffset = new Vector3(0f, 2.5f, -7f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(-0.2f, 0f, 0f),
            dollySpeed     = 0.1f,
            holdDuration   = 5f,
            blendDuration  = 1.5f
        },
        new CameraShot
        {
            positionOffset = new Vector3(1.5f, 2f, -6f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(-0.3f, 0f, 0f),
            dollySpeed     = 0.15f,
            holdDuration   = 5f,
            blendDuration  = 1.5f
        }
    };

    [Header("Enemy Shots")]
    public List<CameraShot> enemyShots = new List<CameraShot>()
    {
        new CameraShot
        {
            positionOffset = new Vector3(1.5f, 2f, -6f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(-0.3f, 0f, 0f),
            dollySpeed     = 0.15f,
            holdDuration   = 4f,
            blendDuration  = 1.5f
        },
        new CameraShot
        {
            positionOffset = new Vector3(0f, 2.5f, -6f),
            lookOffset     = new Vector3(0f, 1f, 0f),
            dollyDirection = new Vector3(0.2f, 0f, 0f),
            dollySpeed     = 0.1f,
            holdDuration   = 4f,
            blendDuration  = 1.5f
        }
    };

    [Header("Targeting Shot")]
    public Vector3 targetingPositionOffset = new Vector3(1.2f, 1.6f, -1.5f);
    public float targetingBlendDuration = 0.4f;

    private Transform activeTarget;
    private Transform secondaryTarget;
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
        secondaryTarget = enemyTarget;
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
        secondaryTarget = playerTarget;
        activeShots = enemyShots;
        BeginShotCycle();
    }

    // TARGETING

    public void EnterTargetingMode()
    {
        if (isTargeting) return;
        if (playerTarget == null || enemyTarget == null)
        {
            Debug.LogWarning("DynamicCameraSystem: playerTarget or enemyTarget is not assigned.");
            return;
        }

        isTargeting = true;

        if (shotCycleCoroutine != null) StopCoroutine(shotCycleCoroutine);
        if (blendCoroutine != null) StopCoroutine(blendCoroutine);

        targetingCoroutine = StartCoroutine(BlendToTargetingShot());
        Debug.Log("DynamicCamera: Entered targeting mode.");
    }

    public void ExitTargetingMode()
    {
        if (!isTargeting) return;
        isTargeting = false;

        if (targetingCoroutine != null) StopCoroutine(targetingCoroutine);

        activeTarget = playerTarget;
        secondaryTarget = enemyTarget;
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

            Vector3 targetPos = playerTarget.position + targetingPositionOffset;
            Vector3 lookDir = (enemyTarget.position + Vector3.up) - targetPos;

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, Quaternion.LookRotation(lookDir), t);

            yield return null;
        }

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

            if (activeTarget == null) yield break;

            CameraShot shot = activeShots[currentShotIndex];
            dollyAccumulator = Vector3.zero;

            blendCoroutine = StartCoroutine(BlendToShot(shot));
            yield return blendCoroutine;

            float elapsed = 0f;
            float maxDolly = 0.5f; // clamp how far the dolly can drift
            Quaternion smoothRot = transform.rotation;

            while (elapsed < shot.holdDuration)
            {
                if (activeTarget == null) yield break;

                elapsed += Time.deltaTime;
                dollyAccumulator += shot.dollyDirection * shot.dollySpeed * Time.deltaTime;

                // Clamp dolly so it never drifts too far
                if (dollyAccumulator.magnitude > maxDolly)
                    dollyAccumulator = dollyAccumulator.normalized * maxDolly;

                Vector3 basePos = activeTarget.position + shot.positionOffset + dollyAccumulator;

                if (secondaryTarget != null)
                {
                    Vector3 midpoint = (activeTarget.position + secondaryTarget.position) * 0.5f;
                    Vector3 toMidpoint = midpoint - activeTarget.position;
                    basePos += toMidpoint * framingStrength;
                }

                transform.position = basePos;

                // Smooth the look rotation instead of snapping
                Vector3 lookPoint = activeTarget.position + shot.lookOffset;
                if (secondaryTarget != null)
                    lookPoint = Vector3.Lerp(lookPoint, secondaryTarget.position + shot.lookOffset, framingStrength * 0.5f);

                Quaternion targetRot = Quaternion.LookRotation(lookPoint - transform.position);
                smoothRot = Quaternion.Slerp(smoothRot, targetRot, Time.deltaTime * 3f);
                transform.rotation = smoothRot;

                yield return null;
            }

            currentShotIndex = (currentShotIndex + 1) % activeShots.Count;
        }
    }

    private IEnumerator BlendToShot(CameraShot shot)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float elapsed = 0f;
        while (elapsed < shot.blendDuration)
        {
            if (activeTarget == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / shot.blendDuration);

            // Target position accounts for framing
            Vector3 endPos = activeTarget.position + shot.positionOffset;
            if (secondaryTarget != null)
            {
                Vector3 midpoint = (activeTarget.position + secondaryTarget.position) * 0.5f;
                endPos += (midpoint - activeTarget.position) * framingStrength;
            }

            Vector3 lookPoint = activeTarget.position + shot.lookOffset;
            if (secondaryTarget != null)
                lookPoint = Vector3.Lerp(lookPoint, secondaryTarget.position + shot.lookOffset, framingStrength * 0.5f);

            Quaternion endRot = Quaternion.LookRotation(lookPoint - endPos);

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        if (activeTarget == null) yield break;
    }
}