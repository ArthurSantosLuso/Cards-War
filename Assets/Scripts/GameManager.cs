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

    #endregion
}
