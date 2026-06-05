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