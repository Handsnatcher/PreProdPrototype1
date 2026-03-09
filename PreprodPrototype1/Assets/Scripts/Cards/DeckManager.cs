using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using Unity.VisualScripting;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("UI References")]
    public Transform handParent;                // UI panel with horizontal layout group
    public GameObject cardUIPrefab;             // Prefab with card ui, image and button
    public TextMeshProUGUI deckCountText;       // Shows cards left in deck
    public TextMeshProUGUI discardCountText;    // Shows amount of cards in discard

    [Header("Drawing")]
    public int cardsToDrawPerTurn = 5;

    [Header("Player")]
    [SerializeField] private Player player;

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

    private void OnEnable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.AddListener(OnPlayerTurnStarted);
            TurnManager.Instance.OnPlayerTurnEnd.AddListener(ClearHandVisuals);
        }
    }

    private void OnDisable()
    {
        if(TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.RemoveListener(OnPlayerTurnStarted);
            TurnManager.Instance.OnPlayerTurnEnd.RemoveListener(ClearHandVisuals);
        }
    }

    private void OnPlayerTurnStarted()
    {
        DrawCards(cardsToDrawPerTurn);
    }

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
    }

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

    public void DiscardCard(Card cardToDiscard)
    {
        if (cardToDiscard == null)
        {
            return;
        }

        hand.Remove(cardToDiscard);

        discard.Add(cardToDiscard);

        ClearHandVisuals();

        UpdateDeckUI();
        OnHandChanged?.Invoke();
    }

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
            case Card.CardType.Attack:
                if (player != null)
                {
                    player.playerAttackCardDamage = card.effectValue;
                    player.PlayerAttack();
                }
                break;

            case Card.CardType.Defend:
                if (player!= null)
                {
                    player.playerDefenseCardValue = card.effectValue;
                    player.PlayerDefense();
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

    public void ClearHandVisuals()
    {
        // Destroy all card UI objects
        foreach (Transform child in handParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void UpdateDeckUI()
    {
        if (deckCountText != null)
        {
            deckCountText.text = deck.Count.ToString();
        }

        if (discardCountText != null)
        {
            discardCountText.text = discard.Count.ToString();
        }
    }

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

    private void Start()
    {
        if(TurnManager.Instance != null)
        {
            TurnManager.Instance.OnManaChanged.AddListener(OnManaChanged);
        }

        // This was for testing, pls ignore
        //InitializeForNewCombat();
        //DrawCards(5);
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnManaChanged.RemoveListener(OnManaChanged);
        }
    }
}
