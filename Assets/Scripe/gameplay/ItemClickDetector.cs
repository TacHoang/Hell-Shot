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

        // Lấy outline
        outlineEffect = GetComponentInChildren<Outline>();

        if (outlineEffect == null)
        {
            Debug.LogWarning("[ItemClickDetector] Thiếu Outline trên: " + gameObject.name);
        }
        else
        {
            outlineEffect.enabled = false;
            outlineEffect.OutlineColor = Color.yellow;
            outlineEffect.OutlineWidth = 5f;
            outlineEffect.OutlineMode = Outline.Mode.OutlineAll;
        }

        // Đảm bảo có collider
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = Vector3.one * 2f; // tăng size cho dễ hover
        }
    }

    private void OnMouseEnter()
    {
        if (manager == null) return;

        // Check đúng lượt
        if (!manager.gunManager.IsMyTurn()) return;

        // Check đúng bên
        int myIndex = manager.Runner.IsServer ? 0 : 1;
        bool isMySide = (myIndex == 0 && mySideIsLeft) || (myIndex == 1 && !mySideIsLeft);

        if (!isMySide) return;

        // Hiện viền
        if (outlineEffect != null)
            outlineEffect.enabled = true;

        // ✅ HIỆN TOOLTIP
        manager.ShowTooltip(mySlotIndex, mySideIsLeft);
    }

    private void OnMouseExit()
    {
        if (outlineEffect != null)
            outlineEffect.enabled = false;

        if (manager != null)
            manager.HideTooltip();
    }

    private void OnMouseDown()
    {
        if (manager == null)
            manager = FindObjectOfType<ItemsManager>();

        if (manager == null) return;

        // Check lượt
        if (!manager.gunManager.IsMyTurn()) return;

        // Check bên
        int myIndex = manager.Runner.IsServer ? 0 : 1;
        bool isMySide = (myIndex == 0 && mySideIsLeft) || (myIndex == 1 && !mySideIsLeft);

        if (!isMySide) return;

        // Tắt viền
        if (outlineEffect != null)
            outlineEffect.enabled = false;

        // Ẩn tooltip luôn cho mượt
        manager.HideTooltip();

        // Animation
        transform.DOScale(Vector3.zero, 0.15f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                manager.RequestUseItem(mySlotIndex, mySideIsLeft);
            });
    }
}