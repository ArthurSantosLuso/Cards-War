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

                    CurrentTurnNumber.Value++;

                    foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
                    {
                        PlayerController pc = client.PlayerObject.GetComponent<PlayerController>();
                        if (pc != null)
                        {
                            pc.ModifyManaServeAuthoritative(1);
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

                // TODO: Pass the cardData to the spawnedUnit here so it sets its attack, health & sprite

                // Sync the new deck state back to the client so their UI Card disappears
                SyncDeckToClient(player, clientId);
            }
        }
    }



    #endregion
}
