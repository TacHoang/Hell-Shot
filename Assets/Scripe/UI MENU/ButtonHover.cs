using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening; // nhớ import DOTween

public class ButtonHoverDOTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Text buttonText; // Hoặc TextMeshProUGUI nếu dùng TMPro
    public Color hoverColor = Color.yellow;
    public float hoverScale = 1.2f;
    public float duration = 0.2f;

    private Color originalColor;
    private Vector3 originalScale;

    void Awake()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<Text>();

        originalColor = buttonText.color;
        originalScale = buttonText.transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Kill mọi tween cũ để không bị xung đột
        buttonText.DOKill();
        buttonText.transform.DOKill();

        // Tween màu
        buttonText.DOColor(hoverColor, duration).SetEase(Ease.OutQuad);

        // Tween scale
        buttonText.transform.DOScale(originalScale * hoverScale, duration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.DOKill();
        buttonText.transform.DOKill();

        // Trả về màu gốc
        buttonText.DOColor(originalColor, duration).SetEase(Ease.OutQuad);

        // Trả về scale gốc
        buttonText.transform.DOScale(originalScale, duration).SetEase(Ease.OutQuad);
    }
}