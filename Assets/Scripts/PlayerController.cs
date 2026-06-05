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
            }

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
}