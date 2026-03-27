using UnityEngine;
using DG.Tweening;

public class ItemClickDetector : MonoBehaviour
{
    private ItemsManager manager;
    private int mySlotIndex;
    private bool mySideIsLeft;

    public void Setup(ItemsManager m, int index, bool isLeft)
    {
        manager = m;
        mySlotIndex = index;
        mySideIsLeft = isLeft;

        // Đảm bảo phải có Collider thì tia chuột mới chạm vào được
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    private void OnMouseDown() 
    {
        if (manager == null) manager = FindObjectOfType<ItemsManager>();

        if (manager != null)
        {
            // --- ĐOẠN SỬA: Chặn không cho nhấn nếu không phải lượt của mình ---
            if (!manager.gunManager.IsMyTurn()) return;

            // Kiểm tra bấm đúng phía của mình không (P1 bên trái, P2 bên phải)
            int myIndex = manager.Runner.IsServer ? 0 : 1;
            bool isMySide = (myIndex == 0 && mySideIsLeft) || (myIndex == 1 && !mySideIsLeft);
            if (!isMySide) return;
            // --------------------------------------------------------------

            // Hiệu ứng: Bóp nhỏ lại rồi mới biến mất
            transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() => {
                manager.RequestUseItem(mySlotIndex, mySideIsLeft);
            });
        }
        else
        {
            Debug.LogError("[ItemClickDetector] Không tìm thấy ItemsManager!");
        }
    }
}