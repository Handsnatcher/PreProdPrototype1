using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
    public static TargetingSystem Instance {  get; private set; }

    private Card selectedAttackCard;    // The card currently waiting for a target
    private bool isTargeting = false;

    public bool IsTargeting => isTargeting;
    public Card SelectedAttackCard => selectedAttackCard;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when player clicks an Attack card
    /// </summary>
    /// <param name="card">Selected attack card</param>
    public void StartTargeting(Card card)
    {
        selectedAttackCard = card;
        isTargeting = true;

        DeckManager.Instance?.RefreshCardInteractables();

        // Enter targeting camera
        DynamicCameraSystem cam = FindFirstObjectByType<DynamicCameraSystem>();
        cam?.EnterTargetingMode();

        Debug.Log($"Targeting started with {card.cardName}");
    }

    private void Update()
    {
        if (!isTargeting)
        {
            return;
        }

        // Left click /  M1 to select enemy
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000.0f))
            {
                EnemyBehaviour enemy = hitInfo.collider.GetComponent<EnemyBehaviour>();

                if (enemy != null && enemy.enemyCurrentHealth > 0)
                {
                    ConfirmAttackOn(enemy);
                    return;
                }
            }
        }

        // Right click / M2 to cancel targeting
        if (Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
        }
    }

    /// <summary>
    /// Executes the attck on the targeted enemy
    /// </summary>
    /// <param name="target">The enemy that was clicked on</param>
    private void ConfirmAttackOn(EnemyBehaviour target)
    {
        isTargeting = false;
        DeckManager.Instance?.RefreshCardInteractables();

        // Spend mana and apply damage
        if (selectedAttackCard != null && TurnManager.Instance.TrySpendMana(selectedAttackCard.manaCost))
        {
            target.EnemyTakeDamage(selectedAttackCard.effectValue);
            TurnManager.Instance.UpdateMoveText(Color.red, "Attacked!");

            // Move card to discard (this removes it from hand)
            DeckManager.Instance.DiscardCard(selectedAttackCard);
        }

        StartCoroutine(ExitAfterDelay());
    }

    /// <summary>
    /// Short delay coroutine that exits targeting camera mode
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExitAfterDelay()
    {
        yield return new WaitForSeconds(1);

        DynamicCameraSystem cam = FindFirstObjectByType<DynamicCameraSystem>();
        cam?.ExitTargetingMode();

        selectedAttackCard = null;
    }

    /// <summary>
    /// Cancels targeting mode
    /// Resets state back to before selection
    /// </summary>
    private void CancelTargeting()
    {
        if (!isTargeting)
        {
            return;
        }

        isTargeting = false;
        DeckManager.Instance?.RefreshCardInteractables();

        DynamicCameraSystem cam = FindFirstObjectByType<DynamicCameraSystem>();
        cam?.ExitTargetingMode();

        selectedAttackCard = null;
        Debug.Log("Targeting Cancelled");
    }
}