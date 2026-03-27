using UnityEngine;
using DG.Tweening;

// Lưu ý: QuickOutline thường không dùng namespace, 
// nên ông không cần thêm dòng 'using' nào lạ ở đây cả.

public class ItemClickDetector : MonoBehaviour
{
    private ItemsManager manager;
    private int mySlotIndex;
    private bool mySideIsLeft;

    // Component từ QuickOutline
    private Outline outlineEffect;

    public void Setup(ItemsManager m, int index, bool isLeft)
    {
        manager = m;
        mySlotIndex = index;
        mySideIsLeft = isLeft;

        // Tìm component Outline (QuickOutline)
        outlineEffect = GetComponentInChildren<Outline>();

        // Nếu chưa có thì báo lỗi để ông biết mà kéo vào Prefab
        if (outlineEffect == null)
        {
            Debug.LogWarning("[ItemClickDetector] Chưa có script Outline trên: " + gameObject.name);
        }
        else
        {
            // Mặc định tắt viền khi mới sinh ra
            outlineEffect.enabled = false;
            
            // Chỉnh sơ thông số cho đẹp (ông có thể chỉnh lại trong Inspector)
            outlineEffect.OutlineColor = Color.yellow;
            outlineEffect.OutlineWidth = 5f;
            outlineEffect.OutlineMode = Outline.Mode.OutlineAll;
        }

        // Đảm bảo phải có Collider để bắt tia chuột
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    private void OnMouseEnter()
    {
        if (manager == null || outlineEffect == null) return;

        // CHỈ hiện viền nếu đang là lượt của mình
        if (!manager.gunManager.IsMyTurn()) return;

        // Kiểm tra đúng phía vật phẩm của mình không
        int myIndex = manager.Runner.IsServer ? 0 : 1;
        bool isMySide = (myIndex == 0 && mySideIsLeft) || (myIndex == 1 && !mySideIsLeft);
        
        if (isMySide)
        {
            outlineEffect.enabled = true; // Hiện viền vàng
            // manager.ShowTooltip(mySlotIndex, mySideIsLeft); // Mở dòng này nếu ông có tooltip
        }
    }

    private void OnMouseExit()
    {
        // Tắt viền khi rời chuột
        if (outlineEffect != null) outlineEffect.enabled = false;
        // if (manager != null) manager.HideTooltip();
    }

    private void OnMouseDown() 
    {
        if (manager == null) manager = FindObjectOfType<ItemsManager>();

        if (manager != null)
        {
            // Chặn bấm nếu không phải lượt
            if (!manager.gunManager.IsMyTurn()) return;

            // Kiểm tra phía vật phẩm
            int myIndex = manager.Runner.IsServer ? 0 : 1;
            bool isMySide = (myIndex == 0 && mySideIsLeft) || (myIndex == 1 && !mySideIsLeft);
            if (!isMySide) return;

            // Tắt viền ngay khi click để hiệu ứng mượt
            if (outlineEffect != null) outlineEffect.enabled = false;

            // Hiệu ứng: Bóp nhỏ lại rồi mới gửi lệnh dùng item
            transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() => {
                manager.RequestUseItem(mySlotIndex, mySideIsLeft);
            });
        }
    }
}