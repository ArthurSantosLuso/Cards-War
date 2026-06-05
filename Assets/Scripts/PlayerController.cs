using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerController : NetworkBehaviour
{
    public PlayerDeckState deckState = new PlayerDeckState();

    [Header("UI Visuals")]
    [SerializeField] private GameObject cardUiPrefab;

    private Transform handUiContainer;

    private readonly NetworkVariable<int> _playerIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> _currentMana = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public int ID => _playerIndex.Value;
    public int CurrentMana => _currentMana.Value;

    private GridTile currentHoveredTile;
    private Camera mainCamera;

    public override void OnNetworkSpawn()
    {
        _currentMana.OnValueChanged += OnManaValueChanged;

        if (IsOwner)
        {
            mainCamera = Camera.main;

            if (UiManager.Instance != null)
            {
                handUiContainer = UiManager.Instance.HandContainer;
                UiManager.Instance.UpdateManaText(_currentMana.Value);

                if (UiManager.Instance.EndTurnButton != null)
                {
                    UiManager.Instance.EndTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
                }
            }

            // Listen to turn changes from the Game Manager
            GameManager.Instance.ActivePlayerIndex.OnValueChanged = OnTurnChanged;

            // Listen to own ID assigned byy the server to check if should unlock button at start
            _playerIndex.OnValueChanged += OnPlayerIndexChanged;

            // Ask server to generate the deck
            GameManager.Instance.GeneratePlayerDeckServerRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        _currentMana.OnValueChanged -= OnManaValueChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleTileInteraction();
    }

    public void SetPlayerIndex(int index)
    {
        _playerIndex.Value = index;
    }

    private void HandleTileInteraction()
    {
        Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        GridTile targetedTile = null;

        if (hit.collider != null)
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();

            if (tile != null && tile.OwnerID == _playerIndex.Value)
                targetedTile = tile;
        }

        if (targetedTile == currentHoveredTile) return;

        currentHoveredTile?.SetHoverActive(false);
        currentHoveredTile = targetedTile;
        currentHoveredTile?.SetHoverActive(true);
    }

    private void OnManaValueChanged(int previousValue, int newValue)
    {
        if (IsOwner && UiManager.Instance != null)
        {
            UiManager.Instance.UpdateManaText(newValue);
        }
    }

    public void ModifyManaServeAuthoritative(int amount)
    {
        if (!IsServer) return;
        _currentMana.Value = Mathf.Max(0, _currentMana.Value + amount);
    }

    private void OnPlayerIndexChanged(int previous, int current)
    {
        // When the server assigns the player ID, check if it's their turn right now
        if (IsOwner) UpdateTurnUI(GameManager.Instance.ActivePlayerIndex.Value);
    }

    private void OnTurnChanged(int previous, int current)
    {
        // When the Game Manager changes the turn, update the button state
        // Adicionar aqui depois talvez uma text box que indica o numero do turno
        if (IsOwner) UpdateTurnUI(current);
    }

    private void UpdateTurnUI(int activeTurnId)
    {
        bool isMyTurn = (ID == activeTurnId);
        UiManager.Instance.SetEndTurnButtonInteractable(isMyTurn);
    }

    private void OnEndTurnButtonClicked()
    {
        // Check if it is players turn (even though the button should be disabled)
        if (ID == GameManager.Instance.ActivePlayerIndex.Value)
        {
            // Disable end turn button
            UiManager.Instance.SetEndTurnButtonInteractable(false);
            // Request to end players turn
            GameManager.Instance.RequestEndTurnServerRpc();
        }
    }

    #region RPC Methods

    [ClientRpc]
    public void SyncDeckToClientClientRpc(int[] handInstanceIds, int[] handCardIds, int[] deckInstanceIds, int[] deckCardIds, ClientRpcParams clientRpcParams = default)
    {
        // Reconstruct logical state
        deckState.Hand.Clear();
        deckState.Deck.Clear();

        for (int i = 0; i < handInstanceIds.Length; i++)
        {
            deckState.Hand.Add(new CardInstance(handInstanceIds[i], handCardIds[i], ID));
        }

        for (int i = 0; i < deckInstanceIds.Length; i++)
        {
            deckState.Deck.Enqueue(new CardInstance(deckInstanceIds[i], deckCardIds[i], ID));
        }

        // Clear current UI card elements from previous generation 
        if (handUiContainer != null)
        {
            foreach (Transform child in handUiContainer)
            {
                Destroy(child.gameObject);
            }

            // Instantiate visual UI Card prefabs
            foreach (CardInstance cardInstance in deckState.Hand)
            {
                // Instantiate into the canvas layout group container
                GameObject instantiatedCard = Instantiate(cardUiPrefab, handUiContainer);

                // initialize card here... Description, Image, Cost, Life, Attack...
            }
        }
    }

    #endregion
}