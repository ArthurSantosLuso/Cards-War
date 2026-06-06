using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UnitVisual : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI currentHpText;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetupUI(int attack, int hp, Sprite artwork)
    {
        if (attackText != null) attackText.text = attack.ToString();
        if (currentHpText != null) currentHpText.text = hp.ToString();
        if (spriteRenderer != null) spriteRenderer.sprite = artwork;
    }
}