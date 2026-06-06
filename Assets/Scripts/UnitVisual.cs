using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UnitVisual : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI currentHpText;

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

    public void SetupUI(int attack, int hp)
    {
        if (attackText!= null) attackText.text = attack.ToString();
        if (currentHpText != null) currentHpText.text = hp.ToString();
    }

}
