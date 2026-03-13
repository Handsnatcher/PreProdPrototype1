using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public enum TurnState { PlayerTurn, EnemyTurn, GameOver }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    public EnemyBehaviour enemyBehaviour;

    [Header("Scenes")]
    public string deathSceneName = "DeathScene";
    public string mapSceneName = "MapScene";

    [Header("Mana Settings")]
    public int maxMana = 3;

    [Header("UI")]
    public Button endTurnButton;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI moveText;

    [Header("Text Timing")]
    public float fadeInDuration = 0.4f;
    public float displayDuration = 1.5f;
    public float fadeOutDuration = 0.6f;

    public float moveUpDistance = 50.0f;    //for the card move text

    [Header("Enemies")]
    public float enemyThinkDuration = 2f;
    public EnemyBehaviour[] enemies;

    [Header("State (Read Only)")]
    [SerializeField] private TurnState currentState;
    [SerializeField] private int currentMana;
    [SerializeField] private int turnCount;

    [Header("Turn Events")]
    public UnityEvent OnPlayerTurnStart;
    public UnityEvent OnPlayerTurnEnd;
    public UnityEvent OnEnemyTurnStart;
    public UnityEvent OnEnemyTurnEnd;

    [Header("Mana Events")]
    public UnityEvent<int, int> OnManaChanged;

    public TurnState CurrentState => currentState;
    public int CurrentMana => currentMana;
    public int TurnCount => turnCount;
    public bool IsPlayerTurn => currentState == TurnState.PlayerTurn;

    private float enemyThinkTimer = 0f;
    private bool enemyThinking = false;
    private Coroutine turnTextCoroutine;

    private Coroutine moveTextCoroutine;
    private Vector3 moveTextStartPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (enemyBehaviour == null)
            enemyBehaviour = FindFirstObjectByType<EnemyBehaviour>();

        if (endTurnButton == null)
            endTurnButton = GameObject.Find("EndTurnButton")?.GetComponent<Button>();

        if (turnText == null)
            turnText = GameObject.Find("TurnText")?.GetComponent<TextMeshProUGUI>();

        if (manaText == null)
            manaText = GameObject.Find("ManaText")?.GetComponent<TextMeshProUGUI>();

        if (moveText == null)
        {
            moveText = GameObject.Find("MoveText")?.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            //store starting position
            moveTextStartPos = moveText.rectTransform.localPosition;
            SetMoveTextAlpha(0.0f);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(EndPlayerTurn);
        }

        if (turnText != null)
            SetTurnTextAlpha(0f);

        // DeckManager Initialize for Combat start
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.InitializeForNewCombat();
        }
        else
        {
            Debug.Log("DeckManager not found.");
        }

        SetState(TurnState.PlayerTurn);
        if (DeckManager.Instance.CheckIfEmpty())
        {
            DeckManager.Instance.DrawCards(5);
        }

    }

    void OnDestroy()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(EndPlayerTurn);
    }

    void Update()
    {
        if (enemyThinking)
        {
            enemyThinkTimer -= Time.deltaTime;
            if (enemyThinkTimer <= 0f)
            {
                enemyThinking = false;

                if (enemyBehaviour != null)
                    enemyBehaviour.EnemyTurn();
                else
                    Debug.LogWarning("TurnManager: enemyBehaviour is not assigned.");

                SetState(TurnState.PlayerTurn);
            }
        }
    }

    // STATE MACHINE

    void SetState(TurnState newState)
    {
        if (currentState == TurnState.GameOver && newState != TurnState.GameOver) return;

        currentState = newState;

        switch (currentState)
        {
            case TurnState.PlayerTurn:
                OnEnterPlayerTurn();
                break;
            case TurnState.EnemyTurn:
                OnEnterEnemyTurn();
                break;
            case TurnState.GameOver:
                OnEnterGameOver();
                break;
        }
    }

    void OnEnterPlayerTurn()
    {
        turnCount++;
        RefillMana();
        SetEndTurnButtonActive(true);
        ShowTurnText("Your Turn");
        OnPlayerTurnStart?.Invoke();

        Debug.Log($"[Turn {turnCount}] --- PLAYER TURN ---  Mana: {currentMana}/{maxMana}");
    }

    void OnEnterEnemyTurn()
    {
        SetEndTurnButtonActive(false);
        ShowTurnText("Enemy Turn");
        UpdateManaText();
        OnPlayerTurnEnd?.Invoke();
        OnEnemyTurnStart?.Invoke();

        enemyThinkTimer = enemyThinkDuration;
        enemyThinking = true;

        Debug.Log($"[Turn {turnCount}] --- ENEMY TURN --- thinking for {enemyThinkDuration}s");
    }

    void OnEnterGameOver()
    {
        enemyThinking = false;
        SetEndTurnButtonActive(false);
        UpdateManaText();

        Debug.Log("TurnManager: Game Over.");
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        GlassShatterEffect shatter = FindFirstObjectByType<GlassShatterEffect>();

        if (shatter != null)
        {
            // Disable dynamic camera before anything
            DynamicCameraSystem dynCam = FindFirstObjectByType<DynamicCameraSystem>();
            if (dynCam != null) dynCam.enabled = false;

            // Start loading in background
            AsyncOperation load = SceneManager.LoadSceneAsync(deathSceneName);
            load.allowSceneActivation = false;

            // Trigger shatter - captures screen and spawns shards
            shatter.TriggerShatter();

            // Wait for scene to be ready and shards to start cracking
            while (load.progress < 0.9f)
                yield return null;

            yield return new WaitForSeconds(shatter.crackDuration + 0.2f);

            // Activate scene - shards are already falling
            load.allowSceneActivation = true;

            yield return null;
            yield return null;

            Camera gameOverCam = null;
            Camera combatCam = null;

            Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera c in allCams)
            {
                if (c.gameObject.scene.name == deathSceneName)
                    gameOverCam = c;
                else
                    combatCam = c;
            }

            // Match game over camera position to combat camera
            if (gameOverCam != null && combatCam != null)
            {
                gameOverCam.transform.position = combatCam.transform.position;
                gameOverCam.transform.rotation = combatCam.transform.rotation;

                // Match projection so shards appear the same size
                gameOverCam.orthographic = combatCam.orthographic;
                gameOverCam.fieldOfView = combatCam.fieldOfView;
                gameOverCam.nearClipPlane = combatCam.nearClipPlane;
                gameOverCam.farClipPlane = combatCam.farClipPlane;
            }

            if (gameOverCam != null)
            {
                gameOverCam.clearFlags = CameraClearFlags.SolidColor;
                gameOverCam.cullingMask = -1;
                gameOverCam.depth = 0;
            }

            // Disable combat camera - game over camera takes over
            if (combatCam != null)
                combatCam.enabled = false;

            // Point canvas at game over camera
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceCamera && gameOverCam != null)
                    c.worldCamera = gameOverCam;
            }
        }
        else
        {
            SceneManager.LoadScene(deathSceneName);
        }
    }

    // PUBLIC INTERFACE

    public void EndPlayerTurn()
    {
        if (currentState != TurnState.PlayerTurn)
        {
            Debug.LogWarning("TurnManager: Tried to end player turn outside of PlayerTurn state.");
            return;
        }
        // DeckManager Initialize for Combat start
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.DiscardHand();
        }
        else
        {
            Debug.Log("DeckManager not found.");
        }

        OnPlayerTurnEnd?.Invoke();
        Debug.Log($"[Turn {turnCount}] Player turn ended.");
        SetState(TurnState.EnemyTurn);
    }

    public void NotifyEnemyTurnStarted()
    {
        if (currentState != TurnState.EnemyTurn) return;
        OnEnemyTurnStart?.Invoke();
        Debug.Log($"[Turn {turnCount}] --- ENEMY TURN ---");
    }

    public void EndEnemyTurn()
    {
        if (currentState != TurnState.EnemyTurn)
        {
            Debug.LogWarning("TurnManager: Tried to end enemy turn outside of EnemyTurn state.");
            return;
        }

        OnEnemyTurnEnd?.Invoke();
        Debug.Log($"[Turn {turnCount}] Enemy turn ended.");
        SetState(TurnState.PlayerTurn);
    }

    public void SetGameOver()
    {
        SetState(TurnState.GameOver);
    }

    // MANA

    public bool TrySpendMana(int amount)
    {
        if (currentState != TurnState.PlayerTurn)
        {
            Debug.LogWarning("TurnManager: Cannot spend mana outside of player turn.");
            return false;
        }

        if (currentMana < amount)
        {
            Debug.Log($"TurnManager: Not enough mana. (Have {currentMana}, need {amount})");
            return false;
        }

        currentMana -= amount;
        OnManaChanged?.Invoke(currentMana, maxMana);
        UpdateManaText();

        Debug.Log($"TurnManager: Spent {amount} mana. Remaining: {currentMana}/{maxMana}");
        return true;
    }

    public void GainMana(int amount)
    {
        currentMana = Mathf.Clamp(currentMana + amount, 0, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
        UpdateManaText();

        Debug.Log($"TurnManager: Gained {amount} mana. Current: {currentMana}/{maxMana}");
    }

    private void RefillMana()
    {
        currentMana = maxMana;
        OnManaChanged?.Invoke(currentMana, maxMana);
        UpdateManaText();
    }

    public void SetVictory()
    {
        if (currentState == TurnState.GameOver) return;
        enemyThinking = false;
        SetEndTurnButtonActive(false);
        ShowTurnText("Victory!");
        Debug.Log("TurnManager: Victory!");
        StartCoroutine(LoadMapAfterDelay());
    }

    private IEnumerator LoadMapAfterDelay()
    {
        yield return new WaitForSeconds(fadeInDuration + displayDuration + fadeOutDuration);
        SceneManager.LoadScene(mapSceneName);
    }

    // UI HELPERS

    private void SetEndTurnButtonActive(bool active)
    {
        if (endTurnButton != null)
            endTurnButton.interactable = active;
    }

    private void ShowTurnText(string message)
    {
        if (turnText == null) return;
        if (turnTextCoroutine != null) StopCoroutine(turnTextCoroutine);
        turnText.text = message;
        turnTextCoroutine = StartCoroutine(FadeTurnText());
    }

    private IEnumerator FadeTurnText()
    {
        yield return StartCoroutine(FadeTo(1f, fadeInDuration));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(FadeTo(0f, fadeOutDuration));
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = turnText.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetTurnTextAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        SetTurnTextAlpha(targetAlpha);
    }

    private void SetTurnTextAlpha(float alpha)
    {
        Color c = turnText.color;
        c.a = alpha;
        turnText.color = c;
    }

    private void UpdateManaText()
    {
        if (manaText != null)
            manaText.text = currentState == TurnState.PlayerTurn
                ? $"Mana: {currentMana} / {maxMana}"
                : "Mana: - / -";
    }

    public void UpdateMoveText(Color color, string text)
    {
        if (moveText == null) return;

        if (moveTextCoroutine != null)
        {
            StopCoroutine(moveTextCoroutine);
        }

        moveText.text = text;
        moveText.color = new Color(color.r, color.g, color.b, 0.0f);

        moveText.rectTransform.localPosition = moveTextStartPos;
        moveTextCoroutine = StartCoroutine(AnimateMoveText());
    }

    private void SetMoveTextAlpha(float alpha)
    {
        if (moveText == null) return;
        Color c = moveText.color;
        c.a = alpha;
        moveText.color = c;
    }

    private IEnumerator AnimateMoveText()
    {
        RectTransform moveRect = moveText.rectTransform;
        float totalTime = fadeInDuration + displayDuration + fadeOutDuration;
        float elapsedTime = 0.0f;

        while (elapsedTime < totalTime)
        {
            if (moveText == null) yield break;

            elapsedTime += Time.deltaTime;

            float alpha;
            if (elapsedTime < fadeInDuration)
                alpha = Mathf.Lerp(0.0f, 1.0f, elapsedTime / fadeInDuration);
            else if (elapsedTime < fadeInDuration + displayDuration)
                alpha = 1.0f;
            else
            {
                float fadeTime = elapsedTime - (fadeInDuration + displayDuration);
                alpha = Mathf.Lerp(1.0f, 0.0f, fadeTime / fadeOutDuration);
            }

            SetMoveTextAlpha(alpha);
            moveRect.localPosition = moveTextStartPos + Vector3.up * (moveUpDistance * (elapsedTime / totalTime));

            yield return null;
        }

        if (moveText == null) yield break;
        SetMoveTextAlpha(0.0f);
    }

}