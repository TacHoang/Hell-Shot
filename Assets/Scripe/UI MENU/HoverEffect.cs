using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class HoverEffect_RightScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI label;
    public float scaleAmount = 1.3f;
    public float duration = 0.2f;
    public Color hoverColor = Color.yellow;

    private Vector3 originalScale;
    private Color originalColor;
    private bool isInitialized = false;

    void Awake() 
    {
        // Khởi tạo các giá trị gốc ngay từ đầu
        if (!isInitialized)
        {
            originalScale = label.rectTransform.localScale;
            originalColor = label.color;
            label.rectTransform.pivot = new Vector2(0, 0.5f);
            isInitialized = true;
        }
    }

    // CHÌA KHÓA: Khi nút biến mất (do tắt Canvas), ép nó về bình thường
    void OnDisable()
    {
        StopAllCoroutines();
        if (isInitialized && label != null)
        {
            label.rectTransform.localScale = originalScale;
            label.color = originalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(Animate(label.rectTransform.localScale, originalScale * scaleAmount, label.color, hoverColor));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(Animate(label.rectTransform.localScale, originalScale, label.color, originalColor));
    }

    IEnumerator Animate(Vector3 fromScale, Vector3 toScale, Color fromColor, Color toColor)
    {
        float t = 0;
        while (t < duration)
        {
            // Dùng unscaledDeltaTime để hiệu ứng mượt kể cả khi game bị lag/pause
            t += Time.unscaledDeltaTime; 
            float lerp = t / duration;

            // Dùng SmoothStep để animation nhìn "xịn" hơn (nhanh ở giữa, chậm ở đầu/cuối)
            float smoothLerp = Mathf.SmoothStep(0, 1, lerp);

            label.rectTransform.localScale = Vector3.Lerp(fromScale, toScale, smoothLerp);
            label.color = Color.Lerp(fromColor, toColor, smoothLerp);

            yield return null;
        }

        label.rectTransform.localScale = toScale;
        label.color = toColor;
    }
}