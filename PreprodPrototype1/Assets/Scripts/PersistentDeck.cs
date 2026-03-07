using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentDeck : MonoBehaviour
{
    public static PersistentDeck Instance {  get; private set; }

    [SerializeField] private List<Card> masterDeck = new List<Card>(); // Drag decks starting cards here

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Survive scene loads so player deck remains consistant and can be added to and removed from
    }
    /// <summary>
    /// Adds card to players deck
    /// </summary>
    public void AddCard(Card card)
    {
        if (card !=null)
        {
            masterDeck.Add(card);
        }
    }

    /// <summary>
    /// Removes card from players deck (May not work like I think it does, requires testing)
    /// </summary>
    public void RemoveCard(Card card)
    {
        if (masterDeck.Contains(card))
        {
            masterDeck.Remove(card);
        }
    }

    /// <summary>
    /// Returns a copy of the 'master deck'
    /// </summary>
    public List<Card> GetCurrentDeckCopy()
    {
        return new List<Card>(masterDeck); // gives copy of masterDeck
    }

    // Getter if other systems need to read without modifying
    public IReadOnlyList<Card> MasterDeck => masterDeck.AsReadOnly();
}
