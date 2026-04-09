using Fusion;
using UnityEngine;

public class PlayerActionController : NetworkBehaviour
{
    private Animator _anim;
    
    [Networked] public PlayerRef PlayerOwner { get; set; }

    // Lưu ID vật phẩm để biết đang dùng món nào (4: Soda, 5: Thuốc, 6: Máu)
    private int _currentUsingItemID;

    [Header("Hand Props Setup")]
    [Tooltip("Thứ tự kéo vào: 0-Soda, 1-Pill, 2-Health")]
    public GameObject[] leftProps;  // 3 món gắn ở xương tay trái
    public GameObject[] rightProps; // 3 món gắn ở xương tay phải

    void Awake() 
    {
        _anim = GetComponent<Animator>();
        if (_anim == null) _anim = GetComponentInChildren<Animator>();
        
        HideAllProps(); // Khởi đầu ẩn hết đồ trên tay
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority) 
        {
            RPC_SetOwner(Runner.LocalPlayer);
        }
        ItemsManager.RegisterPlayerController(this);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetOwner(PlayerRef player) => PlayerOwner = player;

    public void SetCurrentItem(int id) => _currentUsingItemID = id;

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayPickupAction(int itemID, bool isRightSide)
    {
        if (_anim == null) return;

        _currentUsingItemID = itemID;
        _anim.SetBool("IsRightSide", isRightSide);
        
        // Kích hoạt animation nhặt
        _anim.SetTrigger("Use" + itemID); 

        // Nếu là đồ uống (4,5,6), gọi thêm Trigger để chuyển sang trạng thái uống tay phải
        if (itemID >= 4)
        {
            _anim.SetTrigger("DrinkTrigger");
        }
    }

    // ==========================================================
    // --- HỆ THỐNG QUẢN LÝ ĐỒ TRÊN TAY (PROPS) ---
    // ==========================================================

    private void HideAllProps()
    {
        foreach (var p in leftProps) if (p) p.SetActive(false);
        foreach (var p in rightProps) if (p) p.SetActive(false);
    }

    private GameObject GetPropFromID(int id, bool isLeft)
    {
        int index = id - 4; // Map ID 4,5,6 thành Index 0,1,2
        if (index < 0 || index >= 3) return null;
        return isLeft ? leftProps[index] : rightProps[index];
    }

    // ==========================================================
    // --- ANIMATION EVENTS (GẮN TRÊN CLIP TRONG UNITY) ---
    // ==========================================================

    /// <summary>
    /// EVENT 1: Tay chạm bàn. Hiện đồ lên tay tương ứng và xóa đồ dưới bàn.
    /// </summary>
    public void OnPickupMoment() 
    {
        bool isRightHandAction = _anim.GetBool("IsRightSide");

        // Hiện món đồ giả lên tay vừa chạm vào bàn
        GameObject pickedProp = GetPropFromID(_currentUsingItemID, !isRightHandAction);
        if (pickedProp) pickedProp.SetActive(true);

        // Chỉ Server thực hiện xóa dữ liệu dưới bàn
        if (Runner.IsServer)
        {
            var itemsManager = FindObjectOfType<ItemsManager>();
            if (itemsManager != null) itemsManager.RealClearItem();
        }
    }

    /// <summary>
    /// EVENT TRÁO ĐỒ: Gọi lúc tay trái thu về và tay phải bắt đầu dơ lên uống.
    /// </summary>
    public void SwitchPropToRightHand()
    {
        bool isRightHandAction = _anim.GetBool("IsRightSide");

        // Nếu ban đầu nhặt bằng tay trái (!isRightHandAction), thì phải tráo sang phải
        if (!isRightHandAction)
        {
            GameObject leftProp = GetPropFromID(_currentUsingItemID, true);
            GameObject rightProp = GetPropFromID(_currentUsingItemID, false);

            if (leftProp) leftProp.SetActive(false);
            if (rightProp) rightProp.SetActive(true);
        }
    }

    /// <summary>
    /// EVENT 2: Nhân vật thực hiện hành động uống (Giữa clip).
    /// </summary>
    public void OnItemEffectMoment() => RPC_ApplyItemEffect(_currentUsingItemID);

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyItemEffect(int itemID)
    {
        var itemsManager = FindObjectOfType<ItemsManager>();
        if (itemsManager != null && itemsManager.itemLogic != null)
        {
            itemsManager.itemLogic.ExecuteItemLogic(itemID, PlayerOwner, false);
        }
    }

    /// <summary>
    /// EVENT 3: Kết thúc hoàn toàn, ẩn đồ trên tay và mở khóa UI.
    /// </summary>
    public void OnActionFinished()
    {
        Debug.Log("<color=yellow>[Event]</color> Animation Finished được gọi!");
        // Gọi RPC để Server mở khóa
        RPC_FinishAction();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FinishAction()
    {
        var itemsManager = FindObjectOfType<ItemsManager>();
        if (itemsManager != null && itemsManager.gunManager != null)
        {
            // Ép biến này về false trên Server
            itemsManager.gunManager.isAnimatingAction = false;
            
            // TIẾP CHIÊU: Reset luôn cả lock cục bộ nếu có
            itemsManager.gunManager.ResetActionLock(); 
        }
    }
}