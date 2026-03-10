using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform button;
    public TMP_Text buttonText;

    Vector3 normalScale;
    Color normalColor;

    void Start()
    {
        normalScale = button.localScale;
        normalColor = buttonText.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Nút to lên
        button.DOScale(normalScale * 1.1f, 0.2f);

        // Chữ chuyển vàng
        buttonText.DOColor(Color.yellow, 0.2f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Nút trở lại
        button.DOScale(normalScale, 0.2f);

        // Chữ về màu cũ
        buttonText.DOColor(normalColor, 0.2f);
    }
}