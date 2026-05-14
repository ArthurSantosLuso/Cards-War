using UnityEngine;
using UnityEngine.EventSystems;

public class RaycasterOwnerSetup : MonoBehaviour
{
    [SerializeField] private Physics2DRaycaster _raycaster;

    public void Initialize(TileOwner localOwner)
    {
        string layerName = localOwner switch
        {
            TileOwner.Player1 => "P1Tiles",
            TileOwner.Player2 => "P2Tiles",
            _ => throw new System.ArgumentOutOfRangeException()
        };

        _raycaster.eventMask = LayerMask.GetMask(layerName);
    }

}
