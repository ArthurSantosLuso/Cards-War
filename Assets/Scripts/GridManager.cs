using Unity.Netcode;
using UnityEngine;

public class GridManager : NetworkBehaviour
{
    [SerializeField] private GridTile[] allTiles;

    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < allTiles.Length; i++)
            allTiles[i].SetOwner(i < 4 ? 0 : 1);
    }
}