using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class HoverEffect_RightScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI label;       // Kéo Text con vào đây
    public float scaleAmount = 1.3f;    // chữ to lên 30%
    public float duration = 0.2f;       // thời gian animation
    public Color hoverColor = Color.yellow;

    private Vector3 originalScale;
    private Color originalColor;

    void Start()
    {
        originalScale = label.rectTransform.localScale;
        originalColor = label.color;

        // Pivot phải bên trái
        label.rectTransform.pivot = new Vector2(0, 0.5f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(Animate(label, originalScale, originalScale * scaleAmount, originalColor, hoverColor));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(Animate(label, label.rectTransform.localScale, originalScale, label.color, originalColor));
    }

    System.Collections.IEnumerator Animate(TextMeshProUGUI txt, Vector3 fromScale, Vector3 toScale, Color fromColor, Color toColor)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            txt.rectTransform.localScale = Vector3.Lerp(fromScale, toScale, lerp);
            txt.color = Color.Lerp(fromColor, toColor, lerp);

            yield return null;
        }

        txt.rectTransform.localScale = toScale;
        txt.color = toColor;
    }
}