using Unity.Netcode;
using UnityEngine;

public sealed class GridTile : NetworkBehaviour
{
    [Header("Grid Info")]
    [SerializeField] private int tileIdx;

    private bool isOccupied = false;

    private Animator _fillAnimator;
    private static readonly int ActiveHash = Animator.StringToHash("Active");

    public int OwnerID { get; private set; }
    public bool IsOccupied => isOccupied;

    private void Awake()
    {
        _fillAnimator = GetComponentInChildren<Animator>() ?? GetComponent<Animator>();
    }

    public void SetHoverActive(bool state)
    {
        if (!IsClient) return;

        _fillAnimator?.SetBool(ActiveHash, state);
    }

    public void SetOwner(int id)
    {
        OwnerID = id;
    }

    public void SetTileOccupied(bool occupied)
    {
        isOccupied = occupied;
    }
}