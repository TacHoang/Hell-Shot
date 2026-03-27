using UnityEngine;
using DG.Tweening; // Nhớ thêm thư viện này

public class RotateUI : MonoBehaviour
{
    public float duration = 2f; // Thời gian xoay hết 1 vòng (càng nhỏ càng nhanh)

    void Start()
    {
        // Xoay vô tận trục Z
        transform.DORotate(new Vector3(0, 0, -360), duration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear)
            .SetUpdate(UpdateType.Normal, true); 
            // 'true' ở trên là isIndependentUpdate: giúp nó chạy bất chấp lag/đơ
    }

    // Khi tắt Panel thì nên giết Tween để tránh lỗi bộ nhớ
    private void OnDisable()
    {
        transform.DOKill();
    }
}