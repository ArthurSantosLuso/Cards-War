using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    #region Singleton Stuff
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    #endregion

    [SerializeField]
    private PlayableCards data; // Data that stores all playable cards

    [SerializeField]
    private PlayableEffects effectsData; // Data that stores all playable effects

    [SerializeField]
    private GameObject unitPrefab;

    private int currentPlayers = 0;
    private int cardsInGame = 0;

    // Track players turn (0 = Player 1 or 1 = Player 2)
    public NetworkVariable<int> ActivePlayerIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> CurrentTurnNumber = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Track round progression
    private int playerFinishedThisRound = 0;

    public int TotalCurrentPlayers => currentPlayers;

    public Card GetCardDefinition(int cardId)
    {
        if (data != null) return data.GetCard(cardId);
        return null;
    }

    public EffectSO GetEffectDefinition(int effectId)
    {
        if (effectsData != null) return effectsData.GetEffect(effectId);
        return null;
    }

    public void SyncDeckToClient(PlayerController player, ulong clientId)
    {
        int[] handInstanceIds = new int[player.deckState.Hand.Count];
        int[] handCardIds = new int[player.deckState.Hand.Count];
        for (int i = 0; i < player.deckState.Hand.Count; i++)
        {
            handInstanceIds[i] = player.deckState.Hand[i].InstanceId;
            handCardIds[i] = player.deckState.Hand[i].CardId;
        }

        int[] deckInstanceIds = new int[player.deckState.Deck.Count];
        int[] deckCardIds = new int[player.deckState.Deck.Count];
        CardInstance[] deckArray = player.deckState.Deck.ToArray();
        for (int i = 0; i < deckArray.Length; i++)
        {
            deckInstanceIds[i] = deckArray[i].InstanceId;
            deckCardIds[i] = deckArray[i].CardId;
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
        };

        player.SyncDeckToClientClientRpc(handInstanceIds, handCardIds, deckInstanceIds, deckCardIds, clientRpcParams);
    }

    private void ResolveCombatServer()
    {
        if (!IsServer) return;

        int[,] lanes = new int[,] { { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };

        // Collect all units so it can check redirect effects
        UnitController[] p1Units = new UnitController[4];
        UnitController[] p2Units = new UnitController[4];
        for (int i = 0; i < 4; i++)
        {
            p1Units[i] = GridManager.Instance.GetTile(lanes[i, 0]).CurrentUnit;
            p2Units[i] = GridManager.Instance.GetTile(lanes[i, 1]).CurrentUnit;
        }

        // Find redirect shields
        UnitController p1Shield = FindRedirectShield(p1Units); // absorbs damage aimed at player 0
        UnitController p2Shield = FindRedirectShield(p2Units); // absorbs damage aimed at player 1

        // Store (target, amount) pairs so deaths mid loop dont skip hits
        var damageQueue = new System.Collections.Generic.List<(UnitController target, int amount)>();
        var playerDamageQueue = new System.Collections.Generic.List<(int playerId, int amount, UnitController shield)>();

        for (int i = 0; i < 4; i++)
        {
            UnitController p1Unit = p1Units[i];
            UnitController p2Unit = p2Units[i];

            if (p1Unit != null)
            {
                if (p1Unit.Effect != null && p1Unit.Effect.AttacksAllLanes(p1Unit))
                {
                    // Attack every enemy lane
                    for (int j = 0; j < 4; j++)
                    {
                        if (p2Units[j] != null)
                        {
                            int dmg = p1Unit.Effect.ModifyOutgoingDamageVsUnit(p1Unit.Attack, p1Unit, p2Units[j]);
                            damageQueue.Add((p2Units[j], dmg));
                        }
                        else
                        {
                            int dmg = p1Unit.Effect != null
                                ? p1Unit.Effect.ModifyOutgoingDamageVsPlayer(p1Unit.Attack, p1Unit)
                                : p1Unit.Attack;
                            playerDamageQueue.Add((1, dmg, p2Shield));
                        }
                    }
                }
                else if (p2Unit != null)
                {
                    int dmg = p1Unit.Effect != null
                        ? p1Unit.Effect.ModifyOutgoingDamageVsUnit(p1Unit.Attack, p1Unit, p2Unit)
                        : p1Unit.Attack;
                    damageQueue.Add((p2Unit, dmg));
                }
                else
                {
                    int dmg = p1Unit.Effect != null
                        ? p1Unit.Effect.ModifyOutgoingDamageVsPlayer(p1Unit.Attack, p1Unit)
                        : p1Unit.Attack;
                    playerDamageQueue.Add((1, dmg, p2Shield));
                }
            }

            // P2 unit attacks
            if (p2Unit != null)
            {
                if (p2Unit.Effect != null && p2Unit.Effect.AttacksAllLanes(p2Unit))
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (p1Units[j] != null)
                        {
                            int dmg = p2Unit.Effect.ModifyOutgoingDamageVsUnit(p2Unit.Attack, p2Unit, p1Units[j]);
                            damageQueue.Add((p1Units[j], dmg));
                        }
                        else
                        {
                            int dmg = p2Unit.Effect != null
                                ? p2Unit.Effect.ModifyOutgoingDamageVsPlayer(p2Unit.Attack, p2Unit)
                                : p2Unit.Attack;
                            playerDamageQueue.Add((0, dmg, p1Shield));
                        }
                    }
                }
                else if (p1Unit != null)
                {
                    int dmg = p2Unit.Effect != null
                        ? p2Unit.Effect.ModifyOutgoingDamageVsUnit(p2Unit.Attack, p2Unit, p1Unit)
                        : p2Unit.Attack;
                    damageQueue.Add((p1Unit, dmg));
                }
                else
                {
                    int dmg = p2Unit.Effect != null
                        ? p2Unit.Effect.ModifyOutgoingDamageVsPlayer(p2Unit.Attack, p2Unit)
                        : p2Unit.Attack;
                    playerDamageQueue.Add((0, dmg, p1Shield));
                }
            }
        }

        // Apply all unit damage simultaneously
        foreach (var (target, amount) in damageQueue)
            target.TakeDamage(amount);

        // Apply player damage (redirect to shield if present)
        foreach (var (playerId, amount, shield) in playerDamageQueue)
        {
            if (shield != null)
                shield.TakeDamage(amount);
            else
                GetPlayerByID(playerId)?.TakeDamageServerAuthoritative(amount);
        }
    }

    // Returns the first unit in a player's lanes that has the RedirectPlayerDamage effect active.
    private UnitController FindRedirectShield(UnitController[] units)
    {
        foreach (var unit in units)
        {
            if (unit == null || unit.Effect == null) continue;
            if (unit.Effect.RedirectsPlayerDamage(unit, out _))
                return unit;
        }
        return null;
    }

    private PlayerController GetPlayerByID(int id)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            PlayerController pc = client.PlayerObject.GetComponent<PlayerController>();
            if (pc != null && pc.ID == id) return pc;
        }
        return null;
    }

    public void TriggerGameOver(int losingPlayerId) 
    { 
        if (!IsServer) return;
        EndGameClientRpc(losingPlayerId);
    }



    #region Rpc Methods

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GeneratePlayerDeckServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            PlayerController player = networkClient.PlayerObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.deckState.Deck.Clear();
                player.deckState.Hand.Clear();

                for (int i = 0; i < 8; i++)
                {
                    int idx = Random.Range(0, data.Cards.Count);
                    CardInstance card = new CardInstance(cardsInGame++, data.Cards[idx].CardId, (int)clientId);
                    player.deckState.Deck.Enqueue(card);
                }

                // Draw 4 cards into the hand
                for (int i = 0; i < 4; i++)
                {
                    player.deckState.Hand.Add(player.deckState.Deck.Dequeue());
                }

                // Extract hand values into arrays for network transmission
                int[] handInstanceIds = new int[player.deckState.Hand.Count];
                int[] handCardIds = new int[player.deckState.Hand.Count];
                for (int i = 0; i < player.deckState.Hand.Count; i++)
                {
                    handInstanceIds[i] = player.deckState.Hand[i].InstanceId;
                    handCardIds[i] = player.deckState.Hand[i].CardId;
                }

                // Extract ceck values into arrays for network transmission
                int[] deckInstanceIds = new int[player.deckState.Deck.Count];
                int[] deckCardIds = new int[player.deckState.Deck.Count];
                CardInstance[] deckArray = player.deckState.Deck.ToArray();
                for (int i = 0; i < deckArray.Length; i++)
                {
                    deckInstanceIds[i] = deckArray[i].InstanceId;
                    deckCardIds[i] = deckArray[i].CardId;
                }

                // Only the client who requested the deck receives this network data
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                };

                // Send data to the client
                player.SyncDeckToClientClientRpc(handInstanceIds, handCardIds, deckInstanceIds, deckCardIds, clientRpcParams);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestEndTurnServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            PlayerController player = networkClient.PlayerObject.GetComponent<PlayerController>();

            if (player != null && player.ID == ActivePlayerIndex.Value)
            {
                playerFinishedThisRound++;

                if (playerFinishedThisRound >= 2)
                {
                    playerFinishedThisRound = 0;
                    ActivePlayerIndex.Value = 0;

                    ResolveCombatServer();

                    CurrentTurnNumber.Value++;

                    foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
                    {
                        PlayerController pc = client.PlayerObject.GetComponent<PlayerController>();
                        if (pc != null)
                        {
                            pc.ModifyManaServeAuthoritative(2);

                            if (pc.deckState.Deck.Count > 0)
                            {
                                if (pc.deckState.Hand.Count < PlayerDeckState.MAX_HAND_SIZE)
                                {
                                    CardInstance drawnCard = pc.deckState.Deck.Dequeue();
                                    pc.deckState.Hand.Add(drawnCard);
                                }
                            }
                            SyncDeckToClient(pc, client.ClientId);
                        }
                    }
                }
                else
                {
                    ActivePlayerIndex.Value++;
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayCardServerRpc(int instanceId, Vector3 placePosition, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            PlayerController player = networkClient.PlayerObject.GetComponent<PlayerController>();

            if (player != null)
            {
                // Is it actually the player turn?
                if (player.ID != ActivePlayerIndex.Value) return;

                // Does this card actually exist in the player hand?
                CardInstance cardToPlay = player.deckState.Hand.Find(c => c.InstanceId == instanceId);
                if (cardToPlay == null) return;

                // Can the player afford it?
                Card cardData = GetCardDefinition(cardToPlay.CardId);
                if (player.CurrentMana < cardData.Cost) return;


                // Reduce mana
                player.ModifyManaServeAuthoritative(-cardData.Cost);

                // Remove the card from the servers copy of the hand
                player.deckState.Hand.Remove(cardToPlay);

                // Spawn the unit on the board
                GameObject spawnedUnit = Instantiate(unitPrefab, placePosition, Quaternion.identity);
                spawnedUnit.GetComponent<NetworkObject>().Spawn(true);

                Collider2D col = Physics2D.OverlapPoint(placePosition);
                if (col != null)
                {
                    GridTile tile = col.GetComponent<GridTile>();
                    if (tile != null)
                    {
                        UnitController unitScript = spawnedUnit.GetComponent<UnitController>();
                        if (unitScript != null)
                        {
                            // Pass the card health, attack, player ID (0 or 1), and tile reference
                            int effectId = cardData.Effect != null ? cardData.Effect.EffectId : -1;
                            unitScript.SetupServer(cardData.Health, cardData.Damage, cardData.CardId, effectId, player.ID, tile);
                        }
                    }
                }

                // Sync the new deck state back to the client so their UI Card disappears
                SyncDeckToClient(player, clientId);
            }
        }
    }

    [ClientRpc]
    private void EndGameClientRpc(int losingPlayerId)
    {
        int localPlayerId = -1;

        var localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObj != null)
        {
            var localController = localPlayerObj.GetComponent<PlayerController>();
            if (localController != null)
            {
                localPlayerId = localController.ID;
            }
        }

        bool isWinner = (localPlayerId != losingPlayerId);

        if (UiManager.Instance != null)
        {
            UiManager.Instance.ShowGameOver(isWinner);
        }
    }

    #endregion
}