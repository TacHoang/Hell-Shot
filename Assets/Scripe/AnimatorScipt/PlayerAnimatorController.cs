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
    [Tooltip("KÉO ĐỒ TỪ HIERARCHY VÀO: 0-Soda, 1-Pill, 2-Health")]
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
        ItemsManager.RegisterPlayerController(this);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetOwner(PlayerRef player) => PlayerOwner = player;

    public void SetCurrentItem(int id) => _currentUsingItemID = id;

    private void RefreshPropsVisibility()
    {
        HideAllProps();
        
        if (NetworkedPropIndex == -1) 
        {
            var itemsManager = FindObjectOfType<ItemsManager>();
            if (itemsManager != null && itemsManager.gunManager != null)
            {
                // Nhả khóa Local cho mọi máy khách
                itemsManager.gunManager.ResetActionLock();

                // Nhả khóa trạng thái trên Server
                if (itemsManager.gunManager.HasStateAuthority)
                {
                    itemsManager.gunManager.isAnimatingAction = false;
                }
            }
            return;
        }

        bool isRightHand = _anim.GetBool("IsRightSide");
        GameObject prop = GetPropFromID(NetworkedPropIndex, !isRightHand);
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
        if (leftProps != null) foreach (var p in leftProps) if (p) p.SetActive(false);
        if (rightProps != null) foreach (var p in rightProps) if (p) p.SetActive(false);

        if (_anim != null)
        {
            Transform rHand = _anim.GetBoneTransform(HumanBodyBones.RightHand);
            Transform lHand = _anim.GetBoneTransform(HumanBodyBones.LeftHand);
            if (rHand) foreach (Transform t in rHand) t.gameObject.SetActive(false);
            if (lHand) foreach (Transform t in lHand) t.gameObject.SetActive(false);
        }
    }

    private GameObject GetPropFromID(int id, bool isLeft)
    {
        int index = id - 4; 
        if (index < 0 || index >= 3) return null;
        return isLeft ? leftProps[index] : rightProps[index];
    }

    // --- ANIMATION EVENTS ---

    public void OnPickupMoment() 
    {
        if (HasStateAuthority) NetworkedPropIndex = _currentUsingItemID;
    }

    public void SwitchPropToRightHand()
    {
        bool isRightSide = _anim.GetBool("IsRightSide");
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
            // 1. Thực thi logic hiệu ứng (Hồi máu/Hỏng súng...)
            if (itemsManager.itemLogic != null)
                itemsManager.itemLogic.ExecuteItemLogic(itemID, PlayerOwner, false);

            // 2. XÓA VẬT PHẨM TRÊN BÀN: Đảm bảo server dọn dẹp ngay khi dùng
            itemsManager.RealClearItem(); 
            
            Debug.Log($"<color=orange>[Server]</color> Đã xóa vật phẩm ID {itemID} khỏi bàn.");
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

        Debug.Log("<color=cyan>[Action]</color> Đã đồng bộ ẩn đồ và mở khóa lượt.");
    }
}