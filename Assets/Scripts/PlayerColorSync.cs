using UnityEngine;
using Unity.Netcode;
using System.Runtime.InteropServices;

public class PlayerColorSync : NetworkBehaviour
{
    private SpriteRenderer spriteRenderer;

    private NetworkVariable<Color> netColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        netColor.OnValueChanged += OnColorChanged;

        spriteRenderer.color = netColor.Value;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = newColor;
        }
    }

    public override void OnNetworkDespawn()
    {
        netColor.OnValueChanged -= OnColorChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            Color randomColor = new Color(Random.value, Random.value, Random.value);
            ChangeColorServerRpc(randomColor);
        }
    }

    [ServerRpc]
    private void ChangeColorServerRpc(Color newColor)
    {
        netColor.Value = newColor;
    }
}
