using Fusion;
using UnityEngine;

public class PlayerActionController : NetworkBehaviour
{
    private Animator _anim;
    
    [Networked] public PlayerRef PlayerOwner { get; set; }

    // BIẾN MẠNG: Đồng bộ ID vật phẩm. -1 là không cầm gì.
    [Networked, OnChangedRender(nameof(RefreshPropsVisibility))] 
    public int NetworkedPropIndex { get; set; } = -1; 

    private int _currentUsingItemID;

    [Header("Hand Props Setup")]
    [Tooltip("THỨ TỰ KÉO ĐỒ VÀO: \nElement 0: Kính lúp (ID 1)\nElement 1: Soda (ID 4)\nElement 2: Pill (ID 5)\nElement 3: Health (ID 6)")]
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
        
        // Nếu NetworkedPropIndex = -1 -> Ẩn đồ và mở khóa hành động
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
        
        // Chạy Trigger theo ID (VD: Use1, Use4, Use5...)
        _anim.SetTrigger("Use" + itemID); 
        
        // Phân loại hành động
        if (itemID == 1) 
        {
            _anim.SetTrigger("MagnifyTrigger"); // Trigger riêng cho kính lúp
        }
        else if (itemID >= 4) 
        {
            _anim.SetTrigger("DrinkTrigger");   // Trigger cho các loại đồ uống
        }
    }

    private void HideAllProps()
    {
        if (leftProps != null) foreach (var p in leftProps) if (p) p.SetActive(false);
        if (rightProps != null) foreach (var p in rightProps) if (p) p.SetActive(false);
    }

    private GameObject GetPropFromID(int id, bool isLeft)
    {
        int index = -1;
        
        // Logic ánh xạ ID sang Index mảng:
        if (id == 1) index = 0;           // Kính lúp (ID 1) nằm ở Element 0
        else if (id >= 4) index = id - 3;  // ID 4 -> Idx 1, ID 5 -> Idx 2, ID 6 -> Idx 3

        if (index < 0) return null;
        
        if (isLeft)
            return (leftProps != null && index < leftProps.Length) ? leftProps[index] : null;
        else
            return (rightProps != null && index < rightProps.Length) ? rightProps[index] : null;
    }

    // --- ANIMATION EVENTS ---

    public void OnPickupMoment() 
    {
        if (HasStateAuthority) NetworkedPropIndex = _currentUsingItemID;

        var im = ItemsManager.Instance;
        if (im != null && im.networkedPendingSlot != -1)
        {
            im.RPC_HideWorldItemVisual(im.networkedPendingFromLeft, im.networkedPendingSlot);
        }
    }

    public void SwitchPropToRightHand()
    {
        if (NetworkedPropIndex == -1) return; 

        bool isRightSide = _anim.GetBool("IsRightSide");
        if (!isRightSide)
        {
            GameObject leftP = GetPropFromID(_currentUsingItemID, true);
            GameObject rightP = GetPropFromID(_currentUsingItemID, false);
            if (leftP) leftP.SetActive(false);
            if (rightP) rightP.SetActive(true);
        }
    }

    public void OnItemEffectMoment() 
    {
        // RIÊNG KÍNH LÚP: Xử lý bật UI trên kính cho máy của người dùng (Local)
        if (_currentUsingItemID == 1)// && Object.HasInputAuthority)
        {
            HandleMagnifierUILocal();
        }

        RPC_ApplyItemEffect(_currentUsingItemID);
    }

    private void HandleMagnifierUILocal()
    {
        bool isRightSide = _anim.GetBool("IsRightSide");
        GameObject prop = GetPropFromID(1, !isRightSide);

        if (prop == null) {
        Debug.LogError($"[UI Error] Không tìm thấy kính trên tay. RightSide: {isRightSide}");
        return;
    }
        
        if (prop != null)
        {
            // Tìm script UI gắn trên Kính lúp
            var uiHandler = prop.GetComponentInChildren<MagnifierUIHandler>();
            if (uiHandler != null)
            {
                // Lấy dữ liệu đạn từ GunManager (Giả định hàm trả về true nếu đạn thật)
                bool isReal = ItemsManager.Instance.gunManager.GetCurrentBulletType();
                uiHandler.ShowResult(isReal);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyItemEffect(int itemID)
    {
        var itemsManager = FindObjectOfType<ItemsManager>();
        if (itemsManager != null)
        {
            if (itemsManager.itemLogic != null)
                itemsManager.itemLogic.ExecuteItemLogic(itemID, PlayerOwner, false);

            itemsManager.RealClearItem(); 
        }
    }

    public void OnActionFinished() => RPC_FinishAction();

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FinishAction()
    {
        NetworkedPropIndex = -1;
        var itemsManager = FindObjectOfType<ItemsManager>();
        if (itemsManager != null && itemsManager.gunManager != null)
        {
            itemsManager.gunManager.isAnimatingAction = false;
        }
    }
}