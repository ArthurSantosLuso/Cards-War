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

    private NetworkVariable<int> networkEffectId = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int OwnerId { get; private set; }
    public int Health => health.Value;
    public int Attack => attack.Value;
    public int MaxHealth { get; private set; }

    public IEffect Effect { get; private set; }

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
        networkEffectId.OnValueChanged += OnEffectIdChanged;

        ResolveEffect();
        UpdateVisuals();
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;
        attack.OnValueChanged -= OnAttackChanged;
        networkCardId.OnValueChanged -= OnCardIdChanged;
        networkEffectId.OnValueChanged -= OnEffectIdChanged;
    }

    private void OnHealthChanged(int previousValue, int newValue) => UpdateVisuals();
    private void OnAttackChanged(int previousValue, int newValue) => UpdateVisuals();
    private void OnCardIdChanged(int previousValue, int newValue) => UpdateVisuals();
    private void OnEffectIdChanged(int previousValue, int newValue) => ResolveEffect();

    // Each client resolves the IEffect from its local PlayableEffects registry using the synced ID
    private void ResolveEffect()
    {
        if (networkEffectId.Value == -1)
        {
            Effect = null;
            return;
        }
        Effect = GameManager.Instance.GetEffectDefinition(networkEffectId.Value);
    }

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

    public void SetupServer(int hp, int atk, int cardId, int effectId, int owner, GridTile tile)
    {
        if (!IsServer) return;

        MaxHealth = hp;
        health.Value = hp;
        attack.Value = atk;
        networkCardId.Value = cardId;
        networkEffectId.Value = effectId;
        OwnerId = owner;

        occupiedTile = tile;
        occupiedTile.SetUnit(this);

        // Resolve effect immediately on the server too
        ResolveEffect();
    }

    /// <summary>
    /// Server authoritative HP setter used by effects.
    /// </summary>
    public void SetHealth(int newHp)
    {
        if (!IsServer) return;
        health.Value = Mathf.Max(0, newHp);
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;

        // Give the defender's effect a chance to modify incoming damage (e.g. Dodge)
        int finalAmount = Effect != null
            ? Effect.ModifyIncomingDamage(amount, this)
            : amount;

        health.Value -= finalAmount;

        if (health.Value <= 0)
            DieServer();
    }

    private void DieServer()
    {
        // If have the effect, cancel death
        if (Effect != null && Effect.OnDeath(this))
            return; // Effect handled it

        if (occupiedTile != null)
            occupiedTile.SetUnit(null);

        GetComponent<NetworkObject>().Despawn(true);
    }
}