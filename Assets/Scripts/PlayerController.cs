using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public PlayerDeckState deckState = new PlayerDeckState();

    [Header("UI Visuals")]
    [SerializeField] private GameObject cardUiPrefab;
    private Transform _handUiContainer;

    private readonly NetworkVariable<int> _playerIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int ID => _playerIndex.Value;

    private GridTile _currentHoveredTile;
    private Camera _mainCamera;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _mainCamera = Camera.main;

            GameObject containerObj = GameObject.Find("HandContainer");
            if (containerObj != null)
            {
                _handUiContainer = containerObj.transform;
            }
            else
            {
                Debug.LogError("Could not find a GameObject named 'HandContainer' in the scene!");
            }

            // Ask server to generate the deck
            GameManager.Instance.GeneratePlayerDeckServerRpc();
        }
    }

    public void SetPlayerIndex(int index)
    {
        _playerIndex.Value = index;
    }

    private float timer;

    private void Update()
    {
        if (!IsOwner) return;

        HandleTileInteraction();
    }

    private void HandleTileInteraction()
    {
        Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        GridTile targetedTile = null;

        if (hit.collider != null)
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();

            if (tile != null && tile.OwnerID == _playerIndex.Value)
                targetedTile = tile;
        }

        if (targetedTile == _currentHoveredTile) return;

        _currentHoveredTile?.SetHoverActive(false);
        _currentHoveredTile = targetedTile;
        _currentHoveredTile?.SetHoverActive(true);
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
        if (_handUiContainer != null)
        {
            foreach (Transform child in _handUiContainer)
            {
                Destroy(child.gameObject);
            }

            // Instantiate visual UI Card prefabs
            foreach (CardInstance cardInstance in deckState.Hand)
            {
                // Instantiate into the canvas layout group container
                GameObject instantiatedCard = Instantiate(cardUiPrefab, _handUiContainer);

                // initialize card here... Description, Image, Cost, Life, Attack...
            }
        }
    }
}