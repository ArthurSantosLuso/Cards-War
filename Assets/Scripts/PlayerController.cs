using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        netPosition.OnValueChanged += (oldPos, newPos) =>
        {
            transform.position = newPos;
        };

        if (IsServer)
        {
            netPosition.Value = transform.position;
        }
        else
        {
            transform.position = netPosition.Value;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(1))
        {
            MovePlayerServerRpc(new Vector3(0f, 2f, 0f));
        }
    }

    [ServerRpc]
    private void MovePlayerServerRpc(Vector3 offset)
    {
        netPosition.Value += offset;
    }

    public override void OnNetworkDespawn()
    {
        netPosition.OnValueChanged -= (oldPos, newPos) => { transform.position = newPos; };
    }
}