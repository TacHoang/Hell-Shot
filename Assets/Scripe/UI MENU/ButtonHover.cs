using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro; // nhớ import TMP

public class ButtonHoverDOTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI buttonText; // dùng TMP
    public Color hoverColor = Color.yellow;
    public float hoverScale = 1.2f;
    public float duration = 0.2f;

    private Color originalColor;
    private Vector3 originalScale;

    void Awake()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        originalColor = buttonText.color;
        originalScale = buttonText.transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonText.DOKill();
        buttonText.transform.DOKill();

        buttonText.DOColor(hoverColor, duration).SetEase(Ease.OutQuad);
        buttonText.transform.DOScale(originalScale * hoverScale, duration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.DOKill();
        buttonText.transform.DOKill();

        buttonText.DOColor(originalColor, duration).SetEase(Ease.OutQuad);
        buttonText.transform.DOScale(originalScale, duration).SetEase(Ease.OutQuad);
    }
}