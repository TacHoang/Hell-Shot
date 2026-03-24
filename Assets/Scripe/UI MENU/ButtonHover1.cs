using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform button;
    public TMP_Text buttonText;

    [Header("Tùy chỉnh thông số")]
    public float scaleMultiplier = 1.2f; // Độ to khi hover (1.2 = 120%)
    public float animationDuration = 0.2f; // Tốc độ nhanh chậm
    public Color hoverColor = Color.yellow;

    private Vector3 normalScale;
    private Color normalColor;
    private bool isHovered = false;

    void Awake()
    {
        // Lưu thông số gốc
        normalScale = button.localScale;
        normalColor = buttonText.color;
    }

    void OnEnable()
    {
        // Khi hiện lại Canvas, ép nó về bình thường ngay lập tức để tránh bị kẹt từ lần trước
        ResetToNormalImmediate();
    }

    void OnDisable()
    {
        // Dừng mọi animation và reset
        ResetToNormalImmediate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        button.DOKill();
        buttonText.DOKill();

        button.DOScale(normalScale * scaleMultiplier, animationDuration).SetUpdate(true);
        buttonText.DOColor(hoverColor, animationDuration).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ResetToNormal();
    }

    // Reset có hiệu ứng mượt
    void ResetToNormal()
    {
        button.DOKill();
        buttonText.DOKill();
        button.DOScale(normalScale, animationDuration).SetUpdate(true);
        buttonText.DOColor(normalColor, animationDuration).SetUpdate(true);
    }

    // Reset ngay lập tức (dùng khi tắt/mở Canvas)
    void ResetToNormalImmediate()
    {
        isHovered = false;
        button.DOKill();
        buttonText.DOKill();
        button.localScale = normalScale;
        buttonText.color = normalColor;
    }

    // Mẹo: Thêm hàm này để chắc chắn nếu chuột không còn ở trên nút thì nó phải về bình thường
    void Update()
    {
        // Nếu script nghĩ là đang hover nhưng thực tế chuột đã ra ngoài (do di quá nhanh)
        if (isHovered && !EventSystem.current.IsPointerOverGameObject())
        {
            ResetToNormal();
            isHovered = false;
        }
    }
}