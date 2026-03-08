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

    [Header("Mana Settings")]
    public int maxMana = 3;

    [Header("UI")]
    public Button endTurnButton;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI moveText;
    public Slider playerHealthSlider;
    public Slider enemyHealthSlider;

    [Header("Text Timing")]
    public float fadeInDuration = 0.4f;
    public float displayDuration = 1.5f;
    public float fadeOutDuration = 0.6f;

    public float moveUpDistance = 50.0f;    //for the card move text

    [Header("Enemy Thinking Delay")]
    public float enemyThinkDuration = 2f;

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

    [Header("Scenes")]
    public string deathSceneName = "DeathScene";

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

        if (playerHealthSlider == null)
            playerHealthSlider = GameObject.Find("PlayerHealthSlider")?.GetComponent<Slider>();

        if (enemyHealthSlider == null)
            enemyHealthSlider = GameObject.Find("EnemyHealthSlider")?.GetComponent<Slider>();

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
        ShowTurnText("Game Over");
        UpdateManaText();

        Debug.Log("TurnManager: Game Over.");
        StartCoroutine(LoadDeathScene());
    }

    private IEnumerator LoadDeathScene()
    {
        // Wait for the Game Over text to finish fading in before switching
        yield return new WaitForSeconds(fadeInDuration + displayDuration);
        SceneManager.LoadScene(deathSceneName);
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

    public void UpdatePlayerHealthSlider(int playerCurrentHealth, int playerMaxHealth)
    {
        if (playerHealthSlider != null)
        {
            playerHealthSlider.maxValue = playerMaxHealth;
            playerHealthSlider.value = playerCurrentHealth;
        }
    }

    //NOTE: probably need to change how this is shown when there are multiple enemies...
    public void UpdateEnemyHealthSlider(int enemyCurrentHealth, int enemyMaxHealth)
    {
        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.maxValue = enemyMaxHealth;
            enemyHealthSlider.value = enemyCurrentHealth;
        }
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
        Color c = moveText.color;
        c.a = alpha;
        moveText.color = c;
    }

    private IEnumerator AnimateMoveText()
    {
        //move text to fade upwards
        RectTransform moveRect = moveText.rectTransform;

        float totalTime = fadeInDuration + displayDuration + fadeOutDuration;
        float elapsedTime = 0.0f;

        //TODO: make it look smoother when brain hurt less
        while (elapsedTime < totalTime)
        {
            elapsedTime += Time.deltaTime;

            float alpha;

            if(elapsedTime >= fadeInDuration)
            {
                alpha = Mathf.Lerp(0.0f, 1.0f, elapsedTime / fadeInDuration);
            }
            else if(elapsedTime < fadeInDuration + displayDuration)
            {
                //set fully visible
                alpha = 1.0f;
            }
            else
            {
                float fadeTime = elapsedTime - (fadeInDuration + displayDuration);
                alpha = Mathf.Lerp(1.0f, 0.0f, fadeTime / fadeOutDuration);

            }

            SetMoveTextAlpha((float)alpha);

            float moveTextProgress = elapsedTime / totalTime;
            moveRect.localPosition = moveTextStartPos + Vector3.up * (moveUpDistance * moveTextProgress);

            yield return null;
        }

        //set transparent
        SetMoveTextAlpha(0.0f);
    }
}