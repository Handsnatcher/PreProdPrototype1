using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using Unity.VisualScripting;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("UI References")]
    public Transform handParent;                // UI panel with horizontal layout group
    public GameObject cardUIPrefab;             // Prefab with card ui, image and button

    [Header("Pile UI")]
    [SerializeField] private Image deckImage;                       // Image for draw deck
    [SerializeField] private TextMeshProUGUI deckCountText;         // Shows cards left in deck
    [SerializeField] private Image discardImage;                    // Image for discard deck
    [SerializeField] private TextMeshProUGUI discardCountText;      // Shows amount of cards in discard

    [Header("Drawing")]
    public int cardsToDrawPerTurn = 5;

    [Header("Player")]
    [SerializeField] private Player player;

    [Header("Targeting")]
    [SerializeField] private TargetingSystem targetingSystem;

    public List<Card> deck      = new List<Card>();  
    public List<Card> hand      = new List<Card>();
    public List<Card> discard   = new List<Card>();

    public UnityEvent OnHandChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.RemoveListener(OnPlayerTurnStarted);
            TurnManager.Instance.OnPlayerTurnEnd.RemoveListener(ClearHandVisuals);
            TurnManager.Instance.OnManaChanged.RemoveListener(OnManaChanged);
        }
    }

    /// <summary>
    /// Called when the player's turn begins. Draws the standard amount of cards
    /// </summary>
    private void OnPlayerTurnStarted()
    {
        DrawCards(cardsToDrawPerTurn);
    }

    /// <summary>
    /// Prepares the deck for a new combat encounter by copying the persistent master deck
    /// Clearing hand/discard and shuffling
    /// </summary>
    public void InitializeForNewCombat()
    {
        // Get fresh copy from persistant deck
        if (PersistentDeck.Instance == null)
        {
            Debug.LogError("Persistent deck missing!");
            return;
        }

        deck = PersistentDeck.Instance.GetCurrentDeckCopy();

        if (deck.Count == 0)
        {
            Debug.LogWarning("Deck is empty");
            return;
        }

        hand.Clear();
        discard.Clear();

        ShuffleDeck();
        UpdateDeckUI();

        //DrawCards(cardsToDrawPerTurn);
    }

    /// <summary>
    /// Shuffles deck using Fisher-yates
    /// </summary>
    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int random = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[random];
            deck[random] = temp;
        }
    }

    /// <summary>
    /// Draws specified number of cards from the deck into hand
    /// Reshuffles discard into deck if deck runs out
    /// Instantiates CardUI objects
    /// </summary>
    /// <param name="count"></param>
    public void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                if (discard.Count == 0)
                {
                    Debug.LogError("Deck and discard pile are both empty. HOW?!");
                    break;
                }

                deck.AddRange(discard);
                discard.Clear();
                ShuffleDeck();
            }

            Card drawn = deck[0];
            deck.RemoveAt(0);
            hand.Add(drawn);

            GameObject uiCard = Instantiate(cardUIPrefab, handParent);
            CardUI cardUI = uiCard.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.Initialize(drawn, this);
            }
        }

        UpdateDeckUI();
        OnHandChanged?.Invoke();
    }

    /// <summary>
    /// Moves card from hand to discard pile and removes its visual
    /// </summary>
    /// <param name="cardToDiscard">The card to discard</param>
    public void DiscardCard(Card cardToDiscard)
    {
        if (cardToDiscard == null)
        {
            return;
        }

        hand.Remove(cardToDiscard);

        discard.Add(cardToDiscard);

        foreach (Transform child in handParent)
        {
            CardUI ui = child.GetComponent<CardUI>();

            if (ui != null && ui.cardData == cardToDiscard)
            {
                Destroy(child.gameObject);
                break;
            }
        }

        UpdateDeckUI();
        OnHandChanged?.Invoke();
    }

    /// <summary>
    /// Moves all card from hand to discard pile and clears hand visuals
    /// Usually called at the end of player turn
    /// </summary>
    public void DiscardHand()
    {
        if (hand.Count == 0)
        {
            return;
        }

        discard.AddRange(hand);
        hand.Clear();

        ClearHandVisuals();

        UpdateDeckUI();

        OnHandChanged?.Invoke();
    }

    /// <summary>
    /// Attempts to play a card from the hand if the player has enough mana
    /// Applies the cards effect, spends mana, moves to discard
    /// </summary>
    /// <param name="card">The card to play</param>
    /// <returns>True if the card was successfully played, false otherwise</returns>
    public bool PlayCard(Card card)
    {
        if (card == null || !hand.Contains(card))
        {
            return false;
        }

        if (!TurnManager.Instance.TrySpendMana(card.manaCost))
        {
            return false;
        }

        // Apply card effects
        switch (card.type)
        {
            case Card.CardType.Defend:
                if (player != null)
                {
                    //player.playerDefenseCardValue = card.effectValue;
                    player.PlayerDefense(card.effectValue);
                }
                break;
        }

        // Move to discard
        hand.Remove(card);
        discard.Add(card);

        UpdateDeckUI();
        OnHandChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// Destroys all CardUI GameObjects currently in the hand parent transform
    /// Used when clearing hand visuals
    /// </summary>
    public void ClearHandVisuals()
    {
        // Destroy all card UI objects
        foreach (Transform child in handParent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Updates the deck/draw and discard pile count display texts
    /// </summary>
    private void UpdateDeckUI()
    {
        // Deck/Draw pile
        if (deckCountText != null)
        {
            deckCountText.text = deck.Count.ToString();
        }

        if (deckImage != null)
        {
            deckImage.enabled = true;
        }

        // Discard pile
        if (discardCountText != null)
        {
            discardCountText.text = discard.Count.ToString();
        }

        if (discardImage != null)
        {
            discardImage.enabled = true;
        }
    }

    /// <summary>
    /// Called when mana changes - updates interactable state of all cards in hand.
    /// </summary>
    /// <param name="current">Current mana</param>
    /// <param name="max">Max mana</param>
    private void OnManaChanged(int current, int max)
    {
        foreach (Transform child in handParent)
        {
            var cardUI = child.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.UpdateInteractable();
            }
        }
    }

    /// <summary>
    /// Starts targeting mode.
    /// If targeting system is null attempts to locate one.
    /// </summary>
    /// <param name="card">The attack card data to enter targeting mode with</param>
    public void StartAttackTargeting(Card card)
    {
        if (card == null)
        {
            return;
        }

        if (TurnManager.Instance?.CurrentMana < card.manaCost)
        {
            return;
        }

        if (targetingSystem == null)
        {
            targetingSystem = GetComponent<TargetingSystem>() ?? FindFirstObjectByType<TargetingSystem>();
        }

        if (targetingSystem != null)
        {
            targetingSystem.StartTargeting(card);
        }
        else
        {
            Debug.Log("TargetingSystem missing");
        }
    }

    /// <summary>
    /// Forces all CardUI elements currently in the hand to refresh their interactivity.
    /// </summary>
    public void RefreshCardInteractables()
    {
        foreach(Transform child in handParent)
        {
            var cardUI = child.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.UpdateInteractable();
            }
        }
    }

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.AddListener(OnPlayerTurnStarted);
            TurnManager.Instance.OnPlayerTurnEnd.AddListener(ClearHandVisuals);
            TurnManager.Instance.OnManaChanged.AddListener(OnManaChanged);
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.RemoveListener(OnPlayerTurnStarted);
            TurnManager.Instance.OnPlayerTurnEnd.RemoveListener(ClearHandVisuals);
            TurnManager.Instance.OnManaChanged.RemoveListener(OnManaChanged);
        }
    }
}
