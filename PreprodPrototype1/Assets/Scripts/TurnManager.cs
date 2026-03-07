using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public enum TurnState { PlayerTurn, EnemyTurn, GameOver }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    public EnemyBehaviour enemyBehaviour;

    [Header("Mana Settings")]
    public int maxMana = 3;

    [Header("UI")]
    public Button endTurnButton;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI manaText;

    [Header("Turn Text Timing")]
    public float fadeInDuration = 0.4f;
    public float displayDuration = 1.5f;
    public float fadeOutDuration = 0.6f;

    [Header("State (Read Only)")]
    [SerializeField] private TurnState currentState;
    [SerializeField] private int currentMana;
    [SerializeField] private int turnCount;

    // -- Events other systems subscribe to --
    [Header("Turn Events")]
    public UnityEvent OnPlayerTurnStart;
    public UnityEvent OnPlayerTurnEnd;
    public UnityEvent OnEnemyTurnStart;
    public UnityEvent OnEnemyTurnEnd;

    [Header("Mana Events")]
    public UnityEvent<int, int> OnManaChanged;   // (current, max)

    // -- Public read-only accessors --
    public TurnState CurrentState => currentState;
    public int CurrentMana => currentMana;
    public int TurnCount => turnCount;
    public bool IsPlayerTurn => currentState == TurnState.PlayerTurn;

    private Coroutine turnTextCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(EndPlayerTurn);

        // Start the turn text fully transparent
        if (turnText != null)
            SetTurnTextAlpha(0f);

        StartPlayerTurn();
    }

    void OnDestroy()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(EndPlayerTurn);
    }

    // TURN FLOW

    public void StartPlayerTurn()
    {
        if (currentState == TurnState.GameOver) return;

        turnCount++;
        currentState = TurnState.PlayerTurn;

        SetEndTurnButtonActive(true);
        RefillMana();
        ShowTurnText("Your Turn");
        OnPlayerTurnStart?.Invoke();

        Debug.Log($"[Turn {turnCount}] --- PLAYER TURN ---  Mana: {currentMana}/{maxMana}");
    }

    /// <summary>
    /// Call this from your End Turn button or card system.
    /// Also wired up automatically if you assign endTurnButton in the Inspector.
    /// </summary>
    public void EndPlayerTurn()
    {
        Debug.Log("EndPlayerTurn triggered");
        if (currentState != TurnState.PlayerTurn)
        {
            Debug.LogWarning("TurnManager: Tried to end player turn outside of PlayerTurn state.");
            return;
        }

        currentState = TurnState.EnemyTurn;

        SetEndTurnButtonActive(false);
        ShowTurnText("Enemy Turn");
        UpdateManaText();
        OnPlayerTurnEnd?.Invoke();

        Debug.Log($"[Turn {turnCount}] Player turn ended.");
        StartCoroutine(EnemyTurnRoutine());
    }

    IEnumerator EnemyTurnRoutine()
    {
        NotifyEnemyTurnStarted();

        Debug.Log("Enemy thinking......");
        yield return new WaitForSeconds(2.0f);

        enemyBehaviour.EnemyTurn();

        EndEnemyTurn();
    }

    /// <summary>
    /// Called by the enemy system when all enemies have finished acting.
    /// </summary>
    public void EndEnemyTurn()
    {
        if (currentState != TurnState.EnemyTurn)
        {
            Debug.LogWarning("TurnManager: Tried to end enemy turn outside of EnemyTurn state.");
            return;
        }

        OnEnemyTurnEnd?.Invoke();

        Debug.Log($"[Turn {turnCount}] Enemy turn ended.");

        StartPlayerTurn();
    }

    /// <summary>
    /// Called by the enemy system at the start of its turn sequence.
    /// </summary>
    public void NotifyEnemyTurnStarted()
    {
        if (currentState != TurnState.EnemyTurn) return;
        OnEnemyTurnStart?.Invoke();

        Debug.Log($"[Turn {turnCount}] --- ENEMY TURN ---");
    }

    // MANA

    /// <summary>
    /// Attempts to spend mana. Returns true if successful.
    /// Call this from the card system before playing a card.
    /// </summary>
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

    /// <summary>
    /// Adds mana mid-turn (e.g. from a card effect).
    /// </summary>
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

    // GAME OVER

    /// <summary>
    /// Call this from the player or enemy system when combat ends.
    /// </summary>
    public void SetGameOver()
    {
        currentState = TurnState.GameOver;
        SetEndTurnButtonActive(false);
        ShowTurnText("Game Over");
        UpdateManaText();

        Debug.Log("TurnManager: Game Over.");
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

        // Cancel any existing fade so texts don't overlap
        if (turnTextCoroutine != null)
            StopCoroutine(turnTextCoroutine);

        turnText.text = message;
        turnTextCoroutine = StartCoroutine(FadeTurnText());
    }

    private IEnumerator FadeTurnText()
    {
        // Fade in
        yield return StartCoroutine(FadeTo(1f, fadeInDuration));

        // Hold
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        yield return StartCoroutine(FadeTo(0f, fadeOutDuration));
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = turnText.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetTurnTextAlpha(alpha);
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
}