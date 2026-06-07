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

    [Header("Game Over UI")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI endGameText;
    [SerializeField] private Button returnToLobbyButton;

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

    private void Start()
    {
        // Hide the panel at the start of the match
        if (endGamePanel != null) endGamePanel.SetActive(false);

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
        }
    }

    public void UpdateTurnText(int turnNumber)
    {
        if (turnText != null)
        {
            turnText.gameObject.SetActive(true);
            turnText.text = $"{turnNumber}";
        }
    }

    public void UpdateManaText(int amount)
    {
        if (manaText != null)
        {
            manaText.gameObject.SetActive(true);
            manaText.text = $"{amount}";
        }
    }

    public void UpdatePlayerHealthText(int playerId, int currentHealth)
    {
        int localPlayerId = -1;

        var localPlayerObj = Unity.Netcode.NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObj != null)
        {
            var localController = localPlayerObj.GetComponent<PlayerController>();
            if (localController != null)
            {
                localPlayerId = localController.ID;
            }
        }

        // Compare the id being updated to the local player's id
        string hpPrefix = (playerId == localPlayerId) ? "Your HP" : "Enemy HP";

        if (playerId == 0 && player1HealthText != null)
        {
            player1HealthText.gameObject.SetActive(true);
            player1HealthText.text = $"{hpPrefix}: {currentHealth}";
        }
        else if (playerId == 1 && player2HealthText != null)
        {
            player2HealthText.gameObject.SetActive(true);
            player2HealthText.text = $"{hpPrefix}: {currentHealth}";
        }
    }

    public void SetEndTurnButtonInteractable(bool isInteractable)
    {
        if (endTurnButton != null)
        {
            endTurnButton.interactable = isInteractable;
        }
    }

    public void ShowGameOver(bool isWinner)
    {
        if (endGamePanel != null) endGamePanel.SetActive(true);

        if (endGameText != null)
        {
            endGameText.text = isWinner ? "YOU WIN!" : "YOU LOST!";
            endGameText.color = isWinner ? Color.green : Color.red;
        }

        // Lock the end turn button so players can't keep playing
        SetEndTurnButtonInteractable(false);
    }

    private void OnReturnToLobbyClicked()
    {
        // Disconnect from the multiplayer session
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        // Load main menu scene 
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}