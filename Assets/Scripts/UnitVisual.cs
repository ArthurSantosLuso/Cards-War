using Unity.Netcode;
using UnityEngine;

public class UnitVisual : NetworkBehaviour
{
    private void Start()
    {
        Collider2D col = Physics2D.OverlapPoint(transform.position);
        if (col != null)
        {
            GridTile tile = col.GetComponent<GridTile>();
            if (tile != null)
            {
                tile.SetTileOccupied(true);
            }
        }
    }

}
