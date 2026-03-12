using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Components")]
    [SerializeField] private Image fullCardImage;

    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor    = new Color(1f, 0.9f, 0.6f);
    [SerializeField] private Color normalColor      = Color.white;
    [SerializeField] private float scaleOnHover     = 1.2f;
    [SerializeField] private float scaleNormal      = 1.0f;

    public Card cardData;
    private DeckManager deckManager;
    private Button button;
    private RectTransform rectTransform;
    private bool isHovered = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();

        if (button == null)
        {
            Debug.LogError("CardUI needs a Button component!");
        }

        button.onClick.AddListener(OnCardClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnCardClicked);
        }
    }

    /// <summary>
    /// Sets up this card UI with its data and owning DeckManager
    /// </summary>
    /// <param name="card">The card data to display</param>
    /// <param name="manager">The DeckManager that owns this card</param>
    public void Initialize(Card card, DeckManager manager)
    {
        cardData = card;
        deckManager = manager;

        if (fullCardImage != null & cardData.artwork != null)
        {
            fullCardImage.sprite = cardData.artwork;
        }

        // Reset visuals
        UpdateInteractable();

        transform.localScale = Vector3.one * scaleNormal;
    }

    /// <summary>
    /// Updates whether this card is playable based on current turn and mana, AND targeting state
    /// Changes interactable state and tint of the card image
    /// </summary>
    public void UpdateInteractable()
    {
        if (button == null || cardData == null || deckManager == null)
        {
            return;
        }

        bool isPlayerTurn = TurnManager.Instance?.IsPlayerTurn == true;
        bool hasEnoughMana = TurnManager.Instance?.CurrentMana >= cardData.manaCost;
        bool targetingActive = TargetingSystem.Instance?.IsTargeting == true;
        bool isTheSelectedCard = targetingActive && cardData == TargetingSystem.Instance.SelectedAttackCard;

        button.interactable = isPlayerTurn && (!targetingActive || isTheSelectedCard );

        if (isTheSelectedCard)
        {
            fullCardImage.color = selectedColor;
        }
        else if (isPlayerTurn && hasEnoughMana && !targetingActive)
        {
            fullCardImage.color = normalColor;
        }
        else
        {
            fullCardImage.color = new Color(0.6f, 0.6f, 0.6f, 0.9f);
        }

        UpdateScale();
    }

    /// <summary>
    /// Called when the player clicks this card. Attempts to play it via DeckManager
    /// Triggers success/fail visual feedback (fail not working)
    /// </summary>
    private void OnCardClicked()
    {
        if (deckManager == null || cardData == null)
        {
            return;
        }

        bool hasEnoughMana = TurnManager.Instance?.CurrentMana >= cardData.manaCost;

        if (!hasEnoughMana)
        {
            //StartCoroutine(ShakeFeedback(5.0f, 6, 0.05f));
        }

        if (cardData.type == Card.CardType.Attack)
        {
            deckManager.StartAttackTargeting(cardData);
        }
        else
        {

            bool success = deckManager.PlayCard(cardData);

            if (success)
            {
                // Play animation placeholder
                StartCoroutine(PlayCardAnimation());
            }
            else
            {
                // Error animation placeholder
                //StartCoroutine(ShakeFeedback(5.0f, 6, 0.05f));
            }
        }
    }

    /// <summary>
    /// Coroutine that plays a visual animation when a card is successfully played
    /// Scales up, then shrinks and fades out before destroying the GameObject
    /// </summary>
    /// <returns></returns>
    public System.Collections.IEnumerator PlayCardAnimation()
    {
        button.interactable = false;

        float duration = 0.35f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 1.4f;

        // Pop up + glow
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float norm = t / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, norm);
            yield return null;
        }

        // Shrink and fade
        duration = 0.25f;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float norm = t / duration;
            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, norm);

            if (fullCardImage)
            {
                fullCardImage.color = Color.Lerp(normalColor, Color.clear, norm);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Coroustine that plays a shake/vibrate feedback animation when failed to play card
    /// </summary>
    /// <param name="strength">Max offset distance per shake</param>
    /// <param name="shakes">Number of shakes</param>
    /// <param name="durationPerShake">How long each shake is</param>
    /// <returns></returns>
    private System.Collections.IEnumerator ShakeFeedback(float strength, int shakes, float durationPerShake)
    {
        Vector3 originalPos = rectTransform.anchoredPosition;

        for (int i = 0; i < shakes; i++)
        {
            float direction = (i % 2 == 0) ? 1f : -1f;
            float offsetAmount = strength * (1f - (float)i / shakes);

            Vector2 offset = Vector2.right * direction * offsetAmount;
            rectTransform.anchoredPosition = originalPos + (Vector3)offset;

            yield return new WaitForSeconds(durationPerShake);
        }

        rectTransform.anchoredPosition = originalPos;
    }

    /// <summary>
    /// Called when pointer enters this card (hover start)
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable)
            return;

        isHovered = true;
        UpdateScale();
    }

    /// <summary>
    /// Called when pointer exits this card (hover end)
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateScale();
    }

    /// <summary>
    /// Scales card depending on selected / isHovered state
    /// </summary>
    private void UpdateScale()
    {
        bool targetingActive = TargetingSystem.Instance?.IsTargeting == true;
        bool isTheSelectedCard = targetingActive && cardData == TargetingSystem.Instance?.SelectedAttackCard;

        if (isTheSelectedCard || (isHovered && button.interactable))
        {
            transform.localScale = Vector3.one * scaleOnHover;
        }
        else
        {
            transform.localScale = Vector3.one * scaleNormal;
        }
    }
}
