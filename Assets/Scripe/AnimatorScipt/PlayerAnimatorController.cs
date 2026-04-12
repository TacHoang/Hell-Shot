using Fusion;
using UnityEngine;

public class PlayerActionController : NetworkBehaviour
{
    private Animator _anim;
    
    [Header("UI References")]
    public MagnifierUIHandler magnifierUI;

    [Header("Networked Data")]
    [Networked] public PlayerRef PlayerOwner { get; set; }
    [Networked] public int PlayerIndex { get; set; } 
    [Networked, OnChangedRender(nameof(RefreshPropsVisibility))] 
    public int NetworkedPropIndex { get; set; } = -1; 
    
    [Networked, OnChangedRender(nameof(OnGunInHandChanged))]
    public NetworkBool IsHoldingGunVisual { get; set; }

    [Header("Special Visuals")]
    public GameObject handcuffedModel; 
    public GameObject gunInHandProp;   

    [Header("Hand Props Setup")]
    public GameObject[] leftProps;  
    public GameObject[] rightProps; 

    private int _currentUsingItemID;

    void Awake() 
    {
        _anim = GetComponent<Animator>();
        if (_anim == null) _anim = GetComponentInChildren<Animator>();
        HideAllProps();
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority) 
        {
            RPC_SetOwner(Runner.LocalPlayer);
            PlayerIndex = Runner.IsServer ? 0 : 1;
        }
        
        if(ItemsManager.Instance != null) 
            ItemsManager.RegisterPlayerController(this);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetOwner(PlayerRef player) => PlayerOwner = player;

    public void SetCurrentItem(int id) => _currentUsingItemID = id;

    // --- LOGIC HIỂN THỊ VẬT PHẨM ---

    private void RefreshPropsVisibility()
    {
        HideAllProps();
        if (NetworkedPropIndex <= 0) return; 

        // Lúc mới nhặt: Hiện ở tay theo hướng nhặt (isRightSide)
        bool isRightSide = _anim.GetBool("IsRightSide");
        GameObject prop = GetPropFromID(NetworkedPropIndex, !isRightSide);
        if (prop != null) prop.SetActive(true);
    }

    private GameObject GetPropFromID(int id, bool isLeft)
    {
        int index = id - 1; 
        if (index < 0) return null;

        // ÉP BUỘC: Kính (1) và Cưa (2) luôn bên tay PHẢI
        if (id == 1 || id == 2)
        {
            if (rightProps != null && index < rightProps.Length) return rightProps[index];
            return null;
        }

        // Các món khác (Coca, Thuốc...)
        if (isLeft)
            return (leftProps != null && index < leftProps.Length) ? leftProps[index] : null;
        else
            return (rightProps != null && index < rightProps.Length) ? rightProps[index] : null;
    }

    private void HideAllProps()
    {
        if (leftProps != null) foreach (var p in leftProps) if (p) p.SetActive(false);
        if (rightProps != null) foreach (var p in rightProps) if (p) p.SetActive(false);
    }

    // --- ANIMATION EVENTS (DÙNG ĐỂ FIX LỖI CỦA ÔNG) ---

    // Gắn Event này vào lúc bắt đầu Animation "Uống/Dùng" (sau khi nhặt xong)
    // Nó sẽ tắt đồ bên tay trái và bật đồ bên tay phải lên để diễn cảnh uống
    public void SwitchPropToRightHand()
    {
        if (NetworkedPropIndex <= 0) return;

        // Tắt hết
        HideAllProps();

        // Bật đúng món đó ở bên tay PHẢI (isLeft = false)
        GameObject rightProp = GetPropFromID(NetworkedPropIndex, false);
        if (rightProp != null) rightProp.SetActive(true);
    }

    // --- ĐỒNG BỘ CẦM SÚNG ---

    private void OnGunInHandChanged()
    {
        if (gunInHandProp != null) gunInHandProp.SetActive(IsHoldingGunVisual);
        if (ItemsManager.Instance?.gunManager != null)
        {
            var realGun = ItemsManager.Instance.gunManager.rotatingGun;
            if (realGun != null) realGun.SetActive(!IsHoldingGunVisual);
        }
    }

    // --- ACTIONS ---

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayPickupAction(int itemID, bool isRightSide)
    {
        if (_anim == null) return;
        _currentUsingItemID = itemID;
        _anim.SetBool("IsRightSide", isRightSide);
        _anim.SetTrigger("Use" + itemID); 

        if (itemID == 1) _anim.SetTrigger("MagnifyTrigger");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FinishAction()
    {
        NetworkedPropIndex = -1;
        IsHoldingGunVisual = false; 

        if (ItemsManager.Instance != null)
        {
            ItemsManager.Instance.RealClearItem();
            if (ItemsManager.Instance.gunManager != null)
            {
                var gm = ItemsManager.Instance.gunManager;
                gm.isAnimatingAction = false;
                gm.hasShotThisTurn = false;
                gm.UnlockLocalAction();
            }
        }
    }

    public void OnPickupMoment() 
    {
        if (HasStateAuthority) NetworkedPropIndex = _currentUsingItemID;
        var im = ItemsManager.Instance;
        if (im != null && im.networkedPendingSlot != -1)
            im.RPC_HideWorldItemVisual(im.networkedPendingFromLeft, im.networkedPendingSlot);
    }

    public void AnimEvent_StartSawing()
    {
        if (HasStateAuthority && _currentUsingItemID == 2) 
            IsHoldingGunVisual = true;
    }

    public void OnActionFinished() => RPC_FinishAction();
    public void OnItemEffectMoment() => RPC_ApplyItemEffect(_currentUsingItemID);

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyItemEffect(int itemID)
    {
        if (ItemsManager.Instance?.itemLogic != null)
            ItemsManager.Instance.itemLogic.ExecuteItemLogic(itemID, PlayerOwner, false);
    }

    public void OnMagnifierCheck()
    {
        if (!Object.HasInputAuthority) return;
        if (ItemsManager.Instance?.gunManager != null)
        {
            bool isReal = ItemsManager.Instance.gunManager.GetCurrentBulletType(); 
            if (magnifierUI != null) magnifierUI.ShowResult(isReal);
        }
    }

    public override void Render()
    {
        if (ItemsManager.Instance?.gunManager == null) return;
        var gm = ItemsManager.Instance.gunManager;
        bool amITheVictim = gm.isCuffed && gm.cuffedPlayerIndex == PlayerIndex;
        if (handcuffedModel != null && handcuffedModel.activeSelf != amITheVictim)
            handcuffedModel.SetActive(amITheVictim);
        if (_anim != null) _anim.SetBool("IsHandcuffed", amITheVictim); 
    }
}