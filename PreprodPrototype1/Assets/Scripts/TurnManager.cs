// TurnManager.cs
using UnityEngine;
using UnityEngine.Events;

public enum TurnState { PlayerTurn, EnemyTurn, GameOver }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Mana Settings")]
    public int maxMana = 3;

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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        StartPlayerTurn();
    }

    // -------------------------------------------------------------------
    // TURN FLOW
    // -------------------------------------------------------------------

    public void StartPlayerTurn()
    {
        if (currentState == TurnState.GameOver) return;

        turnCount++;
        currentState = TurnState.PlayerTurn;

        RefillMana();
        OnPlayerTurnStart?.Invoke();

        Debug.Log($"[Turn {turnCount}] Player Turn Started -- Mana: {currentMana}/{maxMana}");
    }

    /// <summary>
    /// Call this from your End Turn button or card system.
    /// </summary>
    public void EndPlayerTurn()
    {
        if (currentState != TurnState.PlayerTurn)
        {
            Debug.LogWarning("TurnManager: Tried to end player turn outside of PlayerTurn state.");
            return;
        }

        currentState = TurnState.EnemyTurn;
        OnPlayerTurnEnd?.Invoke();

        Debug.Log($"[Turn {turnCount}] Player Turn Ended");
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

        Debug.Log($"[Turn {turnCount}] Enemy Turn Ended");

        StartPlayerTurn();
    }

    /// <summary>
    /// Called by the enemy system at the start of its turn sequence.
    /// </summary>
    public void NotifyEnemyTurnStarted()
    {
        if (currentState != TurnState.EnemyTurn) return;
        OnEnemyTurnStart?.Invoke();
    }

    // -------------------------------------------------------------------
    // MANA
    // -------------------------------------------------------------------

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
            Debug.Log("TurnManager: Not enough mana.");
            return false;
        }

        currentMana -= amount;
        OnManaChanged?.Invoke(currentMana, maxMana);
        return true;
    }

    /// <summary>
    /// Adds mana mid-turn (e.g. from a card effect).
    /// </summary>
    public void GainMana(int amount)
    {
        currentMana = Mathf.Clamp(currentMana + amount, 0, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    private void RefillMana()
    {
        currentMana = maxMana;
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    // -------------------------------------------------------------------
    // GAME OVER
    // -------------------------------------------------------------------

    /// <summary>
    /// Call this from the player or enemy system when combat ends.
    /// </summary>
    public void SetGameOver()
    {
        currentState = TurnState.GameOver;
        Debug.Log("TurnManager: Game Over.");
    }
}