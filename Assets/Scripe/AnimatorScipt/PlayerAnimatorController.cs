using Fusion;
using UnityEngine;
using System.Linq;
using System.Collections;

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
    
    [Networked, OnChangedRender(nameof(OnGunInLeftHandChanged))]
    public NetworkBool IsHoldingGunInLeftHand { get; set; }

    [Header("Special Visuals")]
    public GameObject handcuffedModel; 
    public GameObject gunInHandProp;   
    public GameObject gunInLeftHandProp; // THÊM CÁI NÀY: Súng tay trái (khi cưa)

    [Header("VFX & Audio")]
    public ParticleSystem flashEffect; 
    public ParticleSystem bloodSplatterEffect; // Thêm cái này: Hiệu ứng máu
    public AudioSource audioSource;    
    public AudioClip realBulletSound;  
    public AudioClip blankBulletSound;

    [Header("Item Audio Clips")]
    public AudioClip glassSound;  // ID 1
    public AudioClip sawSound;    // ID 2
    public AudioClip cuffSound;   // ID 3
    public AudioClip pillSound;   // ID 4, 5, 6 dùng chung (âm thanh uống/nuốt)

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

        var gm = FindFirstObjectByType<GunManager>();
        if (gm != null) gm.RegisterPlayer(this);
    }

    private void PlayItemSound(int id)
    {
        // Thay vì phát luôn, ta gọi Coroutine để chờ
        StartCoroutine(DelayPlaySound(id, 2.0f)); // 1.0f là 1 giây
    }

    private IEnumerator DelayPlaySound(int id, float delayTime)
    {
        // Chờ đúng số giây ông muốn
        yield return new WaitForSeconds(delayTime);

        if (audioSource == null) yield break;

        AudioClip clipToPlay = null;

        switch (id)
        {
            case 1: clipToPlay = glassSound; break;
            case 2: clipToPlay = sawSound; break;
            case 3: clipToPlay = cuffSound; break;
            case 4: 
            case 5: 
            case 6: 
                clipToPlay = pillSound; 
                break;
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    // Hàm thực hiện diễn cảnh té
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayFaint(bool isSelfShot)
    {
        if (_anim == null) return;
        
        // Ép thông số về trạng thái chuẩn trước khi diễn cảnh té
        _anim.SetInteger("ActionID", 0); 
        
        // Gọi đúng tên Trigger ông đã đặt trong Animator
        _anim.SetTrigger(isSelfShot ? "FireMy" : "YouFireMe");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_SetOwner(PlayerRef player) => PlayerOwner = player;

    public void SetCurrentItem(int id) => _currentUsingItemID = id;

    private void RefreshPropsVisibility()
    {
        HideAllProps();
        if (NetworkedPropIndex <= 0) return; 

        bool isRightSide = _anim.GetBool("IsRightSide");
        GameObject prop = GetPropFromID(NetworkedPropIndex, !isRightSide);
        if (prop != null) prop.SetActive(true);
    }

    private GameObject GetPropFromID(int id, bool isLeft)
    {
        int index = id - 1; 
        if (index < 0) return null;

        if (id == 1 || id == 2)
        {
            if (rightProps != null && index < rightProps.Length) return rightProps[index];
            return null;
        }

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

    public void SwitchPropToRightHand()
    {
        if (NetworkedPropIndex <= 0) return;
        HideAllProps();
        GameObject rightProp = GetPropFromID(NetworkedPropIndex, false);
        if (rightProp != null) rightProp.SetActive(true);
    }

    // --- ĐỒNG BỘ CẦM SÚNG (Đã fix tay phải) ---
    // --- ĐỒNG BỘ CẦM SÚNG TAY TRÁI VÀ ẨN SÚNG TRÊN BÀN ---
    private void OnGunInHandChanged()
    {
        // Bật/tắt súng ở tay PHẢI (dùng để bắn)
        if (gunInHandProp != null) 
            gunInHandProp.SetActive(IsHoldingGunVisual);

        // Cập nhật trạng thái súng trên bàn
        RefreshRotatingGunVisibility();
    }
    private void OnGunInLeftHandChanged()
    {
        // Bật/tắt súng ở tay TRÁI (dùng khi cưa)
        if (gunInLeftHandProp != null) 
            gunInLeftHandProp.SetActive(IsHoldingGunInLeftHand);

        // Cập nhật trạng thái súng trên bàn
        RefreshRotatingGunVisibility();
    }

    private void RefreshRotatingGunVisibility()
    {
        if (ItemsManager.Instance?.gunManager != null)
        {
            var realGun = ItemsManager.Instance.gunManager.rotatingGun;
            if (realGun != null)
            {
                // Chỉ hiện súng trên bàn nếu KHÔNG cầm súng phải VÀ KHÔNG cầm súng trái
                bool shouldShowOnTable = !IsHoldingGunVisual && !IsHoldingGunInLeftHand;
                realGun.SetActive(shouldShowOnTable);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayPickupAction(int itemID, bool isRightSide)
    {
        if (_anim == null) return;
        _currentUsingItemID = itemID;
        _anim.SetBool("IsRightSide", isRightSide);
        _anim.SetTrigger("Use" + itemID); 

        if (itemID == 1) _anim.SetTrigger("MagnifyTrigger");

        // THÊM DÒNG NÀY ĐỂ PHÁT ÂM THANH
        PlayItemSound(itemID);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FinishAction()
    {
        // Vì hàm này chạy trên StateAuthority, các dòng dưới đây sẽ có hiệu lực đồng bộ
        IsHoldingGunVisual = false; 
        IsHoldingGunInLeftHand = false;
        NetworkedPropIndex = -1; // Reset vật phẩm trên tay

        if (ItemsManager.Instance != null)
        {
            ItemsManager.Instance.RealClearItem();
            
            if (ItemsManager.Instance.gunManager != null)
            {
                var gm = ItemsManager.Instance.gunManager;
                gm.isAnimatingAction = false; 
                gm.hasShotThisTurn = false; // Mở khóa cho lượt bắn tiếp theo
                gm.UnlockLocalAction();
                
                // QUAN TRỌNG: Cập nhật lại súng trên bàn và xoay về hướng người chơi mới
                gm.RPC_SyncVisuals(); 
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
        // Khi item đang dùng là cái cưa (ID = 2)
        if (HasStateAuthority && _currentUsingItemID == 2) 
        {
            IsHoldingGunVisual = false; // Đảm bảo súng tay phải đang tắt
            IsHoldingGunInLeftHand = true; // Bật súng tay trái & giấu súng trên bàn
        }
    }

    // Gán Event này vào frame cuối cùng của Animation Bắn
    public void OnActionFinished() 
    {
        // Gọi RPC này để tất cả các máy cùng ẩn súng ngay lập tức
        RPC_ForceHideGuns();
        
        // Vẫn gọi cái này để Server dọn dẹp logic (trả súng về bàn, đổi turn)
        RPC_FinishAction();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ForceHideGuns()
    {
        // Ép ẩn tất cả súng trên tay ở local của mỗi máy
        if (gunInHandProp != null) gunInHandProp.SetActive(false);
        if (gunInLeftHandProp != null) gunInLeftHandProp.SetActive(false);
        
        // Ẩn luôn các props khác nếu có
        HideAllProps();

        // Cập nhật lại biến local (Dù là máy Client cũng sẽ thấy súng mất ngay)
        IsHoldingGunVisual = false;
        IsHoldingGunInLeftHand = false;
        NetworkedPropIndex = -1;
    }
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartShootingSequence(bool shootSelf)
    {
        if (_anim == null) return;
        _anim.SetTrigger("StartShoot"); 
        if (shootSelf) _anim.SetTrigger("ShotMe");
        else _anim.SetTrigger("ShotYou");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayFireMe()
    {
        if (_anim != null) _anim.SetTrigger("FireMe");
    }
    public void AnimEvent_PickupGun() 
    {
        if (HasStateAuthority) 
        {
            NetworkedPropIndex = _currentUsingItemID; 
        }

        // THÊM DÒNG NÀY: Ép hiện prop ngay tại máy người chơi cho mượt
        RefreshPropsVisibility(); 

        var im = ItemsManager.Instance;
        if (im != null && im.networkedPendingSlot != -1)
        {
            im.RPC_HideWorldItemVisual(im.networkedPendingFromLeft, im.networkedPendingSlot);
        }
    }

    public void AnimEvent_UnlockOnly()
    {
        if (HasStateAuthority && ItemsManager.Instance?.gunManager != null)
        {
            var gm = ItemsManager.Instance.gunManager;
            gm.isAnimatingAction = false;
            gm.UnlockLocalAction();
        }
    }

    // Hàm này sẽ được gọi từ Animation Event khi tay chạm vào súng trên bàn
    public void AnimEvent_SwapGunFromTableToHand()
    {
        // 1. Chỉ máy có quyền (StateAuthority) mới thay đổi dữ liệu mạng
        if (HasStateAuthority)
        {
            // Bật súng trên tay (Visual súng bắn)
            IsHoldingGunVisual = true;
            
            // Nếu là súng dạng vật phẩm (Item), bật chỉ số prop
            // NetworkedPropIndex = _currentUsingItemID; 
        }

        // 2. Ẩn súng trên bàn ngay lập tức thông qua ItemsManager
        var im = ItemsManager.Instance;
        if (im != null)
        {
            // Ẩn súng ở vị trí slot đang chờ
            if (im.networkedPendingSlot != -1)
            {
                im.RPC_HideWorldItemVisual(im.networkedPendingFromLeft, im.networkedPendingSlot);
            }
            
            // Nếu là cây súng xoay ở giữa bàn (Rotating Gun), ẩn nó đi
            if (im.gunManager != null && im.gunManager.rotatingGun != null)
            {
                // Tạm thời ẩn visual cây súng giữa bàn
                im.gunManager.rotatingGun.SetActive(false);
            }
        }
    }

    public void OnShootMoment() 
    {
        if (HasStateAuthority)
        {
            var gm = ItemsManager.Instance?.gunManager;
            if (gm != null)
            {
                // 1. Kiểm tra loại đạn (Thật/Giả)
                bool isReal = gm.GetCurrentBulletType();

                // 2. Tìm nạn nhân dựa trên targetPlayerIndex trong GunManager
                // (Nếu targetPlayerIndex là index của đối thủ hoặc mình)
                PlayerActionController victim = FindObjectsByType<PlayerActionController>(FindObjectsSortMode.None)
                                                .FirstOrDefault(p => p.PlayerIndex == gm.targetPlayerIndex);

                // 3. Gọi RPC để tất cả mọi người (bao gồm cả mình) thấy hiệu ứng
                RPC_PlayShootEffects(isReal, victim);

                // 4. Gọi lệnh trừ máu cũ của ông
                gm.RPC_ApplyShootDamage();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayShootEffects(bool isReal, PlayerActionController victim)
    {
        if (isReal)
        {
            // HIỆU ỨNG ĐẠN THẬT
            if (flashEffect != null) flashEffect.Play(); // Lửa ở họng súng người bắn
            
            if (audioSource != null && realBulletSound != null)
                audioSource.PlayOneShot(realBulletSound); // Tiếng nổ

            // Máu tóe trên người nạn nhân
            if (victim != null && victim.bloodSplatterEffect != null)
            {
                victim.bloodSplatterEffect.Play();
            }
        }
        else
        {
            // HIỆU ỨNG ĐẠN GIẢ
            if (audioSource != null && blankBulletSound != null)
                audioSource.PlayOneShot(blankBulletSound); // Tiếng "cạch"
                
            // Đạn giả thì không chạy flashEffect và không có máu
        }
    }
}