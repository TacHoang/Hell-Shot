using UnityEngine;
using DG.Tweening;

public class ItemClickDetector : MonoBehaviour
{
    private ItemsManager manager;
    private int mySlotIndex;
    private bool mySideIsLeft;
    private Outline outlineEffect;

    public void Setup(ItemsManager m, int index, bool isLeft)
    {
        manager = m;
        mySlotIndex = index;
        mySideIsLeft = isLeft;
        outlineEffect = GetComponentInChildren<Outline>();
        if (outlineEffect != null) outlineEffect.enabled = false;
    }

    private void OnMouseDown()
    {
        if (manager == null || manager.gunManager == null) return;

        if (!manager.gunManager.CanIInteract()) return;

        int myIndex = manager.Runner.IsServer ? 0 : 1;
        bool isMySide = (myIndex == 0 && mySideIsLeft) || (myIndex == 1 && !mySideIsLeft);
        
        if (!isMySide) return;

        if (outlineEffect != null) outlineEffect.enabled = false;
        manager.HideTooltip();

        // SỬA TẠI ĐÂY: Không DOScale về zero nữa. 
        // Chỉ làm hiệu ứng "nhấn" nhẹ (nảy lên) để người chơi biết là đã click thành công.
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.2f); 

        // Gọi lệnh dùng Item ngay lập tức, vật phẩm vẫn hiển thị trên bàn chờ tay nhân vật tới nhặt.
        manager.RequestUseItem(mySlotIndex, mySideIsLeft);
    }

    private void OnMouseEnter()
    {
        if (manager == null || !manager.gunManager.CanIInteract()) return;
        int myIndex = manager.Runner.IsServer ? 0 : 1;
        if ((myIndex == 0 && mySideIsLeft) || (myIndex == 1 && !mySideIsLeft))
        {
            if (outlineEffect != null) outlineEffect.enabled = true;
            manager.ShowTooltip(mySlotIndex, mySideIsLeft);
        }
    }

    private void OnMouseExit()
    {
        if (outlineEffect != null) outlineEffect.enabled = false;
        if (manager != null) manager.HideTooltip();
    }
}