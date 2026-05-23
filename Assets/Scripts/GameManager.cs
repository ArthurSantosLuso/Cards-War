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

    /// <summary>
    /// Data that store all playable cards. 
    /// </summary>
    public PlayableCards data;

    private int currentPlayers = 0;
    public int TotalCurrentPlayers => currentPlayers;

    public void AddPlayer()
    {
        currentPlayers++;
        //GeneratePlayerDeckRpc();
    }

    //[ServerRpc]
    //public void GeneratePlayerDeckRpc()
    //{
        
    //    for (int i = 0; i < 8; i++)
    //    {
    //        int idx = Random.Range(0, cards.Count);
    //        deck.Enqueue(cards[idx]);
    //    }

    //    for (int i = 0; i < 4; i++)
    //    {
    //        handCards[i] = deck.Dequeue();
    //    }
    //}
}
