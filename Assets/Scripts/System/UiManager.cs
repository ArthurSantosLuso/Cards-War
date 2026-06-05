using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
   public static UiManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform          handContainer;
    [SerializeField] private TextMeshProUGUI    manaText;
    [SerializeField] private Button             endTurnButton;
    [SerializeField] private TextMeshProUGUI    turnText;

    public Transform HandContainer  => handContainer;
    public Button EndTurnButton     => endTurnButton;

    private void Awake()
    {
        if (Instance != null && Instance != null)
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

    public void SetEndTurnButtonInteractable(bool isInteractable)
    {
        if (endTurnButton != null)
        {
            endTurnButton.interactable = isInteractable;
        }
    }
    
}