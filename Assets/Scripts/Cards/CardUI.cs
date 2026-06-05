using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Card UI Component")]
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image cardArtwork;

    [Header("Card Animation")]
    [SerializeField] private Vector2 pivotWhenIdle = new(0, 0.5f);
    [SerializeField] private Vector2 pivotWhenMouseHover = new(0, 0);

    private RectTransform rectTransform;
    private Vector2 targetPivot;

    public CardInstance InstanceData { get; private set; }

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        targetPivot = pivotWhenIdle;
    }

    private void Update()
    {
        rectTransform.pivot = Vector2.Lerp(rectTransform.pivot, targetPivot, 3f * Time.deltaTime);
    }

    public void Setup(Card cardData, CardInstance instanceData)
    {
        InstanceData = instanceData;

        if (cardName != null)           cardName.text = cardData.CardName;
        if (descriptionText != null)    descriptionText.text = cardData.CardDescription;
        if (costText != null)           costText.text = cardData.Cost.ToString();
        if (damageText != null)         damageText.text = cardData.Damage.ToString();
        if (healthText != null)         healthText.text = cardData.Health.ToString();

        if (cardArtwork != null && cardData.Artwork != null)
        {
            cardArtwork.sprite = cardData.Artwork;
        }
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
