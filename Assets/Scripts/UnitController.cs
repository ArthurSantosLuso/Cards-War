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

        UpdateVisuals();
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;
        attack.OnValueChanged -= OnAttackChanged;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateVisuals();
    }

    private void OnAttackChanged(int previousValue, int newValue)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (unitVisual != null)
        {
            unitVisual.SetupUI(attack.Value, health.Value);
        }
    }

    public void SetupServer(int hp, int atk, int owner, GridTile tile)
    {
        if (!IsServer) return;

        health.Value = hp;
        attack.Value = atk;
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
