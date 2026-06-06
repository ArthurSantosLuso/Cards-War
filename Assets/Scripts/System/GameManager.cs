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
    private PlayableCards data; // Data that store all playable cards

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
        if (data != null)
        {
            return data.GetCard(cardId);
        }
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

        // Define the pairs of tiles that make up the 4 opposing lanes
        int[,] lanes = new int[,] { { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };

        for (int i = 0; i < 4; i++)
        {
            GridTile p1Tile = GridManager.Instance.GetTile(lanes[i, 0]);
            GridTile p2Tile = GridManager.Instance.GetTile(lanes[i, 1]);

            UnitController p1Unit = p1Tile.CurrentUnit;
            UnitController p2Unit = p2Tile.CurrentUnit;

            if (p1Unit != null && p2Unit != null)
            {
                // Cache attack values first so dying doesn't prevent dealing damage
                int p1Dmg = p1Unit.Attack;
                int p2Dmg = p2Unit.Attack;

                p1Unit.TakeDamage(p2Dmg);
                p2Unit.TakeDamage(p1Dmg);
            }
            // Only player 1 has a unit in the lane attack player 2 directly
            else if (p1Unit != null && p2Unit == null)
            {
                GetPlayerByID(1)?.TakeDamageServerAuthoritative(p1Unit.Attack);
            }
            // Only player 2 has a unit in the lane attack player 1 directly
            else if (p2Unit != null && p1Unit == null)
            {
                GetPlayerByID(0)?.TakeDamageServerAuthoritative(p2Unit.Attack);
            }
        }
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
                            unitScript.SetupServer(cardData.Health, cardData.Damage, player.ID, tile);
                        }
                    }
                }

                // Sync the new deck state back to the client so their UI Card disappears
                SyncDeckToClient(player, clientId);
            }
        }
    }

    #endregion
}
