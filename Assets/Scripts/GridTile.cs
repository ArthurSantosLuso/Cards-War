using UnityEngine;
using UnityEngine.EventSystems;

public sealed class GridTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Animator _fillAnimator;
    private static readonly int ActiveHash = Animator.StringToHash("Active");

    private void Awake()
    {
        _fillAnimator = GetComponentInChildren<Animator>();
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData _) => SetActive(true);
    void IPointerExitHandler.OnPointerExit(PointerEventData _) => SetActive(false);

    private void SetActive(bool state) => _fillAnimator.SetBool(ActiveHash, state);
}