using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private readonly NetworkVariable<int> _playerIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private GridTile _currentHoveredTile;
    private Camera _mainCamera;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            _mainCamera = Camera.main;
    }
    public void SetPlayerIndex(int index)
    {
        _playerIndex.Value = index;
    }

    private float timer;

    private void Update()
    {
        if (!IsOwner) return;

        ChangeColorClientRpc();
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
    private void ChangeColorClientRpc()
    {
        timer += Time.deltaTime;

        if (timer >= 2)
        {
            GetComponent<SpriteRenderer>().color = UnityEngine.Random.ColorHSV();
            timer = 0;
        }
    }
}