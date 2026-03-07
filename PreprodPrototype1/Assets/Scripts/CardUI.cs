using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
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

    public void UpdateInteractable()
    {
        if (button == null || cardData == null || deckManager == null)
        {
            return;
        }

        bool canPlay =
            TurnManager.Instance?.IsPlayerTurn == true &&
            TurnManager.Instance?.CurrentMana >= cardData.manaCost;

        button.interactable = canPlay;

        if (fullCardImage != null)
        {
            fullCardImage.color = canPlay ? normalColor : new Color(0.6f, 0.6f, 0.6f, 0.9f);
        }
    }

    private void OnCardClicked()
    {
        if (deckManager == null || cardData == null)
        {
            return;
        }

        bool success = deckManager.PlayCard(cardData);

        if (success)
        {
            // Play animation placeholder
            StartCoroutine(PlayCardAnimation());
        }
        else
        {
            // Error animation placeholder
            StartCoroutine(ShakeFeedback(0.12f, 6));
        }
    }

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

    private System.Collections.IEnumerator ShakeFeedback(float strength, int vibrato)
    {
        Vector3 originalPos = rectTransform.anchoredPosition;

        for (int i = 0; i < vibrato; i++)
        {
            rectTransform.anchoredPosition += (Vector2)Random.insideUnitSphere * strength;
            yield return new WaitForSeconds(0.04f);
        }

        rectTransform.anchoredPosition = originalPos;
    }

    public void OnPointerEnter()
    {
        if (!button.interactable)
        {
            return;
        }
        transform.localScale = Vector3.one * scaleOnHover;
    }

    public void OnPointerExit()
    {
        transform.localScale = Vector3.one * scaleNormal;
    }
}
