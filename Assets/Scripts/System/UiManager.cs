using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private TextMeshProUGUI turnText;

    [Header("Global Health UI")]
    [SerializeField] private TextMeshProUGUI player1HealthText;
    [SerializeField] private TextMeshProUGUI player2HealthText;

    public Transform HandContainer => handContainer;
    public Button EndTurnButton => endTurnButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void UpdateTurnText(int turnNumber)
    {
        if (turnText != null)
        {
            turnText.gameObject.SetActive(true);
            turnText.text = $"Turn {turnNumber}";
        }
    }

    public void UpdateManaText(int amount)
    {
        if (manaText != null)
        {
            manaText.gameObject.SetActive(true);
            manaText.text = $"Mana: {amount}";
        }
    }

    public void UpdatePlayerHealthText(int playerId, int currentHealth)
    {
        if (playerId == 0 && player1HealthText != null)
        {
            player1HealthText.gameObject.SetActive(true);
            player1HealthText.text = $"P1 Health: {currentHealth}";
        }
        else if (playerId == 1 && player2HealthText != null)
        {
            player2HealthText.gameObject.SetActive(true);
            player2HealthText.text = $"P2 Health: {currentHealth}";
        }
    }

    public void SetEndTurnButtonInteractable(bool isInteractable)
    {
        if (endTurnButton != null)
        {
            endTurnButton.interactable = isInteractable;
        }
    }
}