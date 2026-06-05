using TMPro;
using UnityEngine;

public class UiManager : MonoBehaviour
{
   public static UiManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform          handContainer;
    [SerializeField] private TextMeshProUGUI    manaText;

    public Transform HandContainer => handContainer;

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
    
}