using Unity.Netcode;
using UnityEngine;

public class UnitController : NetworkBehaviour
{
    private NetworkVariable<int> health = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> attack = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    private NetworkVariable<int> networkCardId = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int OwnerId { get; private set; }
    public int Health => health.Value;
    public int Attack => attack.Value;

    private GridTile occupiedTile;
    private UnitVisual unitVisual;

    private void Awake()
    {
        unitVisual = GetComponent<UnitVisual>();
    }

    public override void OnNetworkSpawn()
    {
        health.OnValueChanged += OnHealthChanged;
        attack.OnValueChanged += OnAttackChanged;
        networkCardId.OnValueChanged += OnCardIdChanged;

        UpdateVisuals();
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;
        attack.OnValueChanged -= OnAttackChanged;
        networkCardId.OnValueChanged -= OnCardIdChanged;
    }

    private void OnHealthChanged(int previousValue, int newValue) => UpdateVisuals();
    private void OnAttackChanged(int previousValue, int newValue) => UpdateVisuals();
    private void OnCardIdChanged(int previousValue, int newValue) => UpdateVisuals();

    private void UpdateVisuals()
    {
        if (unitVisual == null) return;

        Sprite sprite = null;
        if (networkCardId.Value != -1)
        {
            Card cardData = GameManager.Instance.GetCardDefinition(networkCardId.Value);
            if (cardData != null)
                sprite = cardData.Artwork;
        }

        unitVisual.SetupUI(attack.Value, health.Value, sprite);
    }

    public void SetupServer(int hp, int atk, int cardId, int owner, GridTile tile)
    {
        if (!IsServer) return;

        health.Value = hp;
        attack.Value = atk;
        networkCardId.Value = cardId;
        OwnerId = owner;

        occupiedTile = tile;
        occupiedTile.SetUnit(this);
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;

        health.Value -= amount;

        if (health.Value <= 0)
        {
            DieServer();
        }
    }

    private void DieServer()
    {
        if (occupiedTile != null)
        {
            // Clear tile
            occupiedTile.SetUnit(null);
        }
        // Destroy obj
        GetComponent<NetworkObject>().Despawn(true);
    }
}