using Fusion;
using UnityEngine;

public class PlayerActionController : NetworkBehaviour
{
    private Animator _anim;
    
    [Networked] public PlayerRef PlayerOwner { get; set; }

    // BIẾN MẠNG: Đồng bộ ID vật phẩm (4,5,6). -1 là không cầm gì.
    [Networked, OnChangedRender(nameof(RefreshPropsVisibility))] 
    public int NetworkedPropIndex { get; set; } = -1; 

    private int _currentUsingItemID;

    [Header("Hand Props Setup")]
    [Tooltip("KÉO ĐỒ TỪ HIERARCHY VÀO: Element 0: Soda (ID 4), 1: Pill (ID 5), 2: Health (ID 6)")]
    public GameObject[] leftProps;
    public GameObject[] rightProps;

    void Awake() 
    {
        _anim = GetComponent<Animator>();
        if (_anim == null) _anim = GetComponentInChildren<Animator>();
        HideAllProps();
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority) RPC_SetOwner(Runner.LocalPlayer);
        
        // Đảm bảo ItemsManager đã tồn tại trước khi đăng ký
        if(ItemsManager.Instance != null) ItemsManager.RegisterPlayerController(this);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetOwner(PlayerRef player) => PlayerOwner = player;

    public void SetCurrentItem(int id) => _currentUsingItemID = id;

    private void RefreshPropsVisibility()
    {
        HideAllProps();
        
        // Nếu NetworkedPropIndex = -1, nghĩa là hành động đã kết thúc -> Ẩn đồ và mở khóa
        if (NetworkedPropIndex == -1) 
        {
            var itemsManager = FindObjectOfType<ItemsManager>();
            if (itemsManager != null && itemsManager.gunManager != null)
            {
                itemsManager.gunManager.ResetActionLock();
                if (itemsManager.gunManager.HasStateAuthority)
                {
                    itemsManager.gunManager.isAnimatingAction = false;
                }
            }
            return;
        }

        // Kiểm tra xem đang dùng tay nào dựa trên Animator
        bool isRightSide = _anim.GetBool("IsRightSide");
        
        // Bật đúng món đồ trên tay tương ứng
        GameObject prop = GetPropFromID(NetworkedPropIndex, !isRightSide);
        if (prop != null) prop.SetActive(true);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayPickupAction(int itemID, bool isRightSide)
    {
        if (_anim == null) return;
        _currentUsingItemID = itemID;
        _anim.SetBool("IsRightSide", isRightSide);
        _anim.SetTrigger("Use" + itemID); 
        
        if (itemID >= 4) _anim.SetTrigger("DrinkTrigger");
    }

    private void HideAllProps()
    {
        // Chỉ ẩn các object trong danh sách quản lý, không can thiệp vào Bone của Animator
        if (leftProps != null) foreach (var p in leftProps) if (p) p.SetActive(false);
        if (rightProps != null) foreach (var p in rightProps) if (p) p.SetActive(false);
    }

    private GameObject GetPropFromID(int id, bool isLeft)
    {
        int index = id - 4; // ID 4 -> index 0, ID 5 -> index 1...
        if (index < 0 || index >= 3) return null;
        
        if (isLeft)
            return (leftProps != null && index < leftProps.Length) ? leftProps[index] : null;
        else
            return (rightProps != null && index < rightProps.Length) ? rightProps[index] : null;
    }

    // --- ANIMATION EVENTS (Gọi từ Animation Clips) ---

    public void OnPickupMoment() 
    {
        // 1. Đồng bộ vật phẩm lên tay (biến mạng)
        if (HasStateAuthority) NetworkedPropIndex = _currentUsingItemID;

        // 2. Lệnh cho tất cả các máy ẩn vật phẩm dưới bàn đi ngay lập tức
        var im = ItemsManager.Instance;
        if (im != null && im.networkedPendingSlot != -1)
        {
            im.RPC_HideWorldItemVisual(im.networkedPendingFromLeft, im.networkedPendingSlot);
        }
    }

    public void SwitchPropToRightHand()
    {
        // CHỐT CHẶN: Nếu Server đã báo cất đồ (-1), không thực hiện chuyển tay để tránh kẹt đồ
        if (NetworkedPropIndex == -1) return; 

        bool isRightSide = _anim.GetBool("IsRightSide");
        // Nếu đang thực hiện animation chuyển từ trái sang phải (IsRightSide đang là false)
        if (!isRightSide)
        {
            GameObject leftP = GetPropFromID(_currentUsingItemID, true);
            GameObject rightP = GetPropFromID(_currentUsingItemID, false);
            if (leftP) leftP.SetActive(false);
            if (rightP) rightP.SetActive(true);
        }
    }

    public void OnItemEffectMoment() => RPC_ApplyItemEffect(_currentUsingItemID);

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyItemEffect(int itemID)
    {
        var itemsManager = FindObjectOfType<ItemsManager>();
        if (itemsManager != null)
        {
            if (itemsManager.itemLogic != null)
                itemsManager.itemLogic.ExecuteItemLogic(itemID, PlayerOwner, false);

            itemsManager.RealClearItem(); 
            Debug.Log($"<color=orange>[Server]</color> Thực thi hiệu ứng và xóa vật phẩm ID {itemID}");
        }
    }

    public void OnActionFinished() => RPC_FinishAction();

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FinishAction()
    {
        // Reset biến mạng về -1 -> Kích hoạt RefreshPropsVisibility ẩn toàn bộ đồ
        NetworkedPropIndex = -1;

        var itemsManager = FindObjectOfType<ItemsManager>();
        if (itemsManager != null && itemsManager.gunManager != null)
        {
            itemsManager.gunManager.isAnimatingAction = false;
        }

        Debug.Log("<color=cyan>[Action]</color> Kết thúc hành động, đã ẩn đồ.");
    }
}