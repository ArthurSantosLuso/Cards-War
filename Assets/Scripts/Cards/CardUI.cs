using UnityEngine;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Vector2 pivotWhenIdle = new(0, 0.5f);
    [SerializeField] private Vector2 pivotWhenMouseHover = new(0, 0);

    private RectTransform rectTransform;
    private Vector2 targetPivot;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        targetPivot = pivotWhenIdle;
    }

    private void Update()
    {
        rectTransform.pivot = Vector2.Lerp(rectTransform.pivot, targetPivot, 3f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPivot = pivotWhenMouseHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPivot = pivotWhenIdle;
    }
}
