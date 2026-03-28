using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;

public class GunManager : NetworkBehaviour
{
    // --- NETWORK DATA ---
    [Header("Network Data")]
    [Networked, Capacity(8)] public NetworkArray<NetworkBool> bullets { get; }
    [Networked] public int bulletCount { get; set; }
    [Networked] public int player1HP { get; set; }
    [Networked] public int player2HP { get; set; }
    [Networked] public int activePlayerIndex { get; set; }
    [Networked] public int currentRound { get; set; }
    [Networked] public NetworkBool isWaitingNextRound { get; set; }
    [Networked] public NetworkBool doubleDamage { get; set; } 
    [Networked] public NetworkBool isCuffed { get; set; } 
    [Networked] public NetworkBool canStartSequence { get; set; }
    [Networked] public NetworkBool hasShotThisTurn { get; set; } // Biến mới chặn dùng đồ

    // --- GAMEPLAY REFERENCES ---
    [Header("Core References")]
    public GameObject rotatingGun; 
    public Transform muzzlePoint; 
    public ItemsManager itemsManager; 
    public int maxHP = 5;

    [Header("Cinematic & Timer Settings")]
    public float zoomFOV = 30f;    
    public float normalFOV = 60f;  
    public float zoomDuration = 1.5f;
    public float roundDisplayDuration = 2.0f; 

    [Header("UI References")]
    public HealthBarController hpUI; 
    public GameObject shotCanvas; 
    public TextMeshProUGUI waitingText; 
    public GameObject roundPanel; 
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI targetText; 
    private CanvasGroup roundCanvasGroup;

    // --- INTERNAL STATES ---
    private bool isShotCanvasVisible = false;
    private bool isWaitingTextVisible = false;

    [Header("Mouse Sway Settings")]
    public float swayAmount = 2.0f; // Độ nghiêng tối đa (độ)
    public float swaySmoothing = 5.0f; // Độ mượt khi liếc
    private Quaternion baseRotation; // Lưu góc nhìn chuẩn sau khi súng dừng
    private bool canSway = false; // Chỉ cho phép liếc sau khi súng quay xong
    private float breatheTimer; // Biến này để tính nhịp thở

    public override void Spawned()
    {
        if (roundPanel != null) 
            roundCanvasGroup = roundPanel.GetComponent<CanvasGroup>();

        if (shotCanvas != null) { shotCanvas.SetActive(false); shotCanvas.transform.localScale = Vector3.zero; }
        if (waitingText != null) { waitingText.gameObject.SetActive(false); waitingText.transform.localScale = Vector3.zero; }
        if (targetText != null) targetText.gameObject.SetActive(false);

        if (HasStateAuthority)
        {
            player1HP = maxHP;
            player2HP = maxHP;
            currentRound = 1;
            doubleDamage = false;
            isCuffed = false;
            canStartSequence = false; 
            hasShotThisTurn = false;

            StartCoroutine(MasterStartSequence());
        }
    }

    IEnumerator MasterStartSequence()
    {
        while (!canStartSequence) yield return null;
        isWaitingNextRound = true; 
        RPC_StartGunSpin(); 
        yield return new WaitForSeconds(11.5f); 

        if (HasStateAuthority) 
        {
            if (hpUI != null) RPC_TriggerHealthIntro(); 
            yield return new WaitForSeconds(1.5f); 
            RPC_PlayRoundEffect(currentRound);
            yield return new WaitForSeconds(roundDisplayDuration); 
            StartCoroutine(NextRoundRoutine(true)); 
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_StartGunSpin() { StartCoroutine(GunSpinRoutine()); }

IEnumerator GunSpinRoutine()
{
    Camera currentActiveCam = Camera.main; 
    if (rotatingGun != null && currentActiveCam != null)
    {
        // --- 1. CHUẨN BỊ: TẮT SWAY VÀ DỪNG CÁC TWEEN CŨ ---
        canSway = false; // Khóa liếc chuột để diễn cảnh xoay súng không bị giật
        currentActiveCam.transform.DOKill(); 
        
        // Lưu lại hướng nhìn ban đầu làm "tâm" cho hiệu ứng Sway sau này
        Quaternion originalRotation = currentActiveCam.transform.rotation;
        
        rotatingGun.transform.DOKill();

        // --- 2. XOAY LIẾC QUA SÚNG VÀ ZOOM VÀO ---
        currentActiveCam.transform.DOLookAt(rotatingGun.transform.position, zoomDuration)
            .SetEase(Ease.InOutSine)
            .SetLink(currentActiveCam.gameObject);

        currentActiveCam.DOFieldOfView(zoomFOV, zoomDuration)
            .SetEase(Ease.InOutSine)
            .SetLink(currentActiveCam.gameObject);
        
        // Logic quay súng chọn lượt (10 giây)
        float startZ = rotatingGun.transform.localEulerAngles.z;
        float targetTotalZ = startZ + 3600f + Random.Range(0f, 360f); 

        float currentZ = startZ;
        DOTween.To(() => currentZ, x => currentZ = x, targetTotalZ, 10f)
            .SetEase(Ease.OutQuart)
            .SetLink(rotatingGun)
            .OnUpdate(() => {
                rotatingGun.transform.localRotation = Quaternion.Euler(90f, 0f, currentZ);
            });

        // Đợi súng quay xong
        yield return new WaitForSeconds(10.5f);

        // Hiển thị Text thông báo kết quả
        if (targetText != null)
        {
            float finalZ = rotatingGun.transform.localEulerAngles.z % 360f;
            if (finalZ < 0) finalZ += 360f;
            int pointingAtPlayer = (finalZ > 180f) ? 0 : 1;
            int myLocalIndex = (Runner.IsServer) ? 0 : 1;

            targetText.text = (pointingAtPlayer == myLocalIndex) ? "BẠN ĐI TRƯỚC" : "HỌ ĐI TRƯỚC";
            targetText.color = (pointingAtPlayer == myLocalIndex) ? Color.green : Color.red;

            targetText.gameObject.SetActive(true);
            targetText.transform.localScale = Vector3.zero;
            Sequence textSeq = DOTween.Sequence().SetLink(targetText.gameObject);
            textSeq.Append(targetText.transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack));
            textSeq.AppendInterval(1.0f);
            textSeq.Append(targetText.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack));
            textSeq.OnComplete(() => targetText.gameObject.SetActive(false));
        }

        // Hiệu ứng rung súng khi dừng lại
        rotatingGun.transform.DOShakeRotation(0.5f, new Vector3(0, 0, 10), 10, 90).SetLink(rotatingGun);

        // --- 3. TRẢ CAMERA VỀ VỊ TRÍ CŨ ---
        currentActiveCam.DOFieldOfView(normalFOV, 1f).SetEase(Ease.OutBack).SetLink(currentActiveCam.gameObject);
        
        // Quan trọng: Đợi quay về hẳn vị trí cũ rồi mới cho phép Sway
        yield return currentActiveCam.transform.DORotateQuaternion(originalRotation, 1f)
            .SetEase(Ease.OutExpo)
            .SetLink(currentActiveCam.gameObject)
            .WaitForCompletion(); 

        // --- 4. KÍCH HOẠT CHẾ ĐỘ THỞ VÀ LIẾC CHUỘT TRONG UPDATE ---
        baseRotation = originalRotation; // Chốt góc nhìn chuẩn làm tâm
        breatheTimer = 0f;               // Reset nhịp thở về 0
        canSway = true;                  // Bật đèn xanh cho hàm Update xử lý Sway + Breath

        yield return new WaitForSeconds(0.8f);

        if (HasStateAuthority)
        {
            float finalZ = rotatingGun.transform.localEulerAngles.z % 360f;
            if (finalZ < 0) finalZ += 360f;
            int winner = (finalZ > 180f) ? 0 : 1;
            RPC_FinalizeWinner(winner);
        }
    }
}

    public IEnumerator NextRoundRoutine(bool isFirstRound = false) {
        if (!HasStateAuthority) yield break;
        isWaitingNextRound = true; 
        hasShotThisTurn = false; // Reset trạng thái khi sang Round mới
        GenerateBullets();

        if (!isFirstRound) 
        {
            yield return new WaitForSeconds(1f);
            RPC_PlayRoundEffect(currentRound);
            yield return new WaitForSeconds(roundDisplayDuration + 0.5f); 
            ChangeTurn();
        }
        else yield return new WaitForSeconds(0.5f);

        int itemsToGive = (currentRound == 1) ? 2 : Mathf.Min(currentRound, 4);
        if (itemsManager != null && itemsToGive > 0) 
        {
            itemsManager.GiveRandomItemsToBoth(itemsToGive);
            yield return new WaitForSeconds(1.5f); 
        }

        RPC_SyncVisuals();
        isWaitingNextRound = false; 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayRoundEffect(int roundNumber) {
        if (roundPanel == null || roundCanvasGroup == null) return;
        roundText.text = "ROUND " + roundNumber; 
        roundPanel.SetActive(true);
        roundCanvasGroup.alpha = 0f; 
        roundCanvasGroup.DOFade(1f, 0.5f).SetLink(roundPanel).OnComplete(() => {
            roundCanvasGroup.DOFade(0f, 0.5f).SetDelay(roundDisplayDuration).OnComplete(() => roundPanel.SetActive(false));
        });
    }

void Update() 
{
    // Kiểm tra an toàn
    if (Object == null || Runner == null) return;

    // --- 1. LOGIC HIỂN THỊ UI (BẮN & CHỜ) ---
    bool gameInProgress = canStartSequence && !isWaitingNextRound && (hpUI == null || !hpUI.isAnimating);
    bool isMyTurn = IsMyTurn();

    // HIỂN THỊ NÚT BẮN
    bool shouldShowShot = gameInProgress && isMyTurn && !hasShotThisTurn;
    if (shouldShowShot != isShotCanvasVisible) {
        isShotCanvasVisible = shouldShowShot;
        shotCanvas.transform.DOKill();
        if (shouldShowShot) {
            shotCanvas.SetActive(true);
            shotCanvas.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetLink(shotCanvas);
        } else {
            shotCanvas.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetLink(shotCanvas).OnComplete(() => shotCanvas.SetActive(false));
        }
    }

    // HIỂN THỊ CHỮ CHỜ
    bool shouldShowWaiting = gameInProgress && !isMyTurn;
    if (shouldShowWaiting != isWaitingTextVisible) {
        isWaitingTextVisible = shouldShowWaiting;
        waitingText.transform.DOKill();
        if (shouldShowWaiting) {
            waitingText.gameObject.SetActive(true);
            waitingText.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetLink(waitingText.gameObject);
        } else {
            waitingText.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetLink(waitingText.gameObject).OnComplete(() => waitingText.gameObject.SetActive(false));
        }
    }

    // --- 2. LOGIC CAMERA (LIẾC CHUỘT + NHỊP THỞ TOÁN HỌC) ---
    if (canSway && Camera.main != null)
    {
        // A. TÍNH NHỊP THỞ (Sử dụng hàm Sin để tạo chuyển động lên xuống)
        breatheTimer += Time.deltaTime;
        // 1.5f là độ mạnh (biên độ), 2.0f là tốc độ thở. Ông có thể chỉnh 2 số này.
        float breatheOffset = Mathf.Sin(breatheTimer * 1.2f) * 1f;

        // B. TÍNH VỊ TRÍ CHUỘT
        float mouseX = (Input.mousePosition.x / Screen.width) * 2 - 1;
        float mouseY = (Input.mousePosition.y / Screen.height) * 2 - 1;

        // C. KẾT HỢP: Lấy góc chuột cộng thêm góc thở vào trục X
        // Công thức: (-mouseY * swayAmount) là góc chuột, cộng thêm breatheOffset là góc thở
        Quaternion targetSway = Quaternion.Euler((-mouseY * swayAmount) + breatheOffset, mouseX * swayAmount, 0);
        
        // D. ÁP DỤNG MƯỢT MÀ
        Camera.main.transform.rotation = Quaternion.Slerp(
            Camera.main.transform.rotation, 
            baseRotation * targetSway, 
            Time.deltaTime * swaySmoothing
        );
    }
}
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] void RPC_FinalizeWinner(int winnerIdx) { activePlayerIndex = winnerIdx; RotateGunToActivePlayer(); }
    
    void RotateGunToActivePlayer() { 
        if (rotatingGun == null) return; 
        rotatingGun.transform.DOKill(); 
        float targetZ = (activePlayerIndex == 0) ? -90f : 90f; 
        rotatingGun.transform.DORotate(new Vector3(90f, 0f, targetZ), 0.8f).SetEase(Ease.OutBack).SetLink(rotatingGun); 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] void RPC_TriggerHealthIntro() { if (hpUI != null) hpUI.StartHealthIntro(); }
    
    void GenerateBullets() { 
        if (!HasStateAuthority) return; 
        List<bool> temp = new List<bool>(); 
        if (currentRound == 1) AddBulletsToList(temp, 1, 1); 
        else AddBulletsToList(temp, Random.Range(2, 5), Random.Range(2, 5)); 
        for (int i = 0; i < temp.Count; i++) { 
            int r = Random.Range(i, temp.Count); 
            (temp[i], temp[r]) = (temp[r], temp[i]); 
        } 
        for (int i = 0; i < temp.Count; i++) bullets.Set(i, temp[i]); 
            bulletCount = temp.Count; 
    }

    void AddBulletsToList(List<bool> list, int real, int blank) { for (int i = 0; i < real; i++) list.Add(true); for (int i = 0; i < blank; i++) list.Add(false); }
    
    public void RequestShoot(bool shootSelf) { if (IsMyTurn() && !isWaitingNextRound && !hasShotThisTurn && (hpUI == null || !hpUI.isAnimating)) RPC_Shoot(shootSelf); }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
    public void RPC_Shoot(bool shootSelf) { 
        if (bulletCount <= 0 || isWaitingNextRound || hasShotThisTurn) return; 
        
        hasShotThisTurn = true; // KHÓA: Đã bóp cò thì không được dùng đồ nữa
        bool isReal = bullets[0]; 
        bool isLast = (bulletCount == 1); 
        
        for (int i = 0; i < bulletCount - 1; i++) bullets.Set(i, bullets[i + 1]); 
        bulletCount--; 
        
        int dmg = doubleDamage ? 2 : 1; 
        doubleDamage = false; 
        bool change = true; 
        
        if (isReal) { 
            if (activePlayerIndex == 0) { 
                if (shootSelf) player1HP -= dmg; else player2HP -= dmg; 
            } else { 
                if (shootSelf) player2HP -= dmg; else player1HP -= dmg; 
            } 
        } else if (shootSelf) {
            change = false; 
            hasShotThisTurn = false; // ĐẶC BIỆT: Bắn mình đạn giả thì được hồi lại lượt dùng đồ
        }
        
        player1HP = Mathf.Max(0, player1HP); 
        player2HP = Mathf.Max(0, player2HP); 
        RPC_AnimateHealth(player1HP, player2HP); 

        if (isLast) { 
            isWaitingNextRound = true; 
        } else if (change) {
            ChangeTurn(); 
        }

        RPC_SyncVisuals(); 
        
        if (isLast && player1HP > 0 && player2HP > 0) { 
            currentRound++; 
            StartCoroutine(WaitForHealthThenRound()); 
        } 
        CheckGameOver(); 
    }

    IEnumerator WaitForHealthThenRound() { yield return new WaitForSeconds(4.0f); StartCoroutine(NextRoundRoutine()); }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] void RPC_SyncVisuals() => RotateGunToActivePlayer();
    
    // --- CÁC HÀM ITEM (Có chặn Waiting) ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_Cua() { if(!isWaitingNextRound && !hasShotThisTurn) doubleDamage = true; }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_CongTay() { if(!isWaitingNextRound && !hasShotThisTurn) isCuffed = true; }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
    public void RPC_UseItem_NuocNgot() { 
        if (isWaitingNextRound || hasShotThisTurn || bulletCount <= 0) return; 
        bool last = (bulletCount == 1); 
        for (int i = 0; i < bulletCount - 1; i++) bullets.Set(i, bullets[i + 1]); 
        bulletCount--; 
        if (last && player1HP > 0 && player2HP > 0) { 
            isWaitingNextRound = true; 
            currentRound++; 
            StartCoroutine(NextRoundRoutine()); 
        } 
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_BinhMau() { if (isWaitingNextRound || hasShotThisTurn) return; if (activePlayerIndex == 0) player1HP = Mathf.Min(player1HP + 1, maxHP); else player2HP = Mathf.Min(player2HP + 1, maxHP); RPC_AnimateHealth(player1HP, player2HP); }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_LoThuoc() { if (isWaitingNextRound || hasShotThisTurn) return; int h = (Random.Range(0, 2) == 0) ? 1 : -1; if (activePlayerIndex == 0) player1HP = Mathf.Clamp(player1HP + h, 0, maxHP); else player2HP = Mathf.Clamp(player2HP + h, 0, maxHP); RPC_AnimateHealth(player1HP, player2HP); CheckGameOver(); }
    
    void ChangeTurn() { 
        hasShotThisTurn = false; // Reset trạng thái bóp cò cho người mới
        if (isCuffed) { isCuffed = false; return; } 
        activePlayerIndex = (activePlayerIndex == 0) ? 1 : 0; 
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] void RPC_AnimateHealth(int p1, int p2) { StopCoroutine("HealthAnimationSequence"); StartCoroutine(HealthAnimationSequence(p1, p2)); }
    
    IEnumerator HealthAnimationSequence(int p1, int p2) { 
        if (hpUI == null) yield break; 
        UpdateUIWithSpecificHP(p1, p2); 
        yield return hpUI.StartCoroutine(hpUI.ShowHealthGroups()); 
        yield return new WaitForSeconds(2.0f); 
        yield return hpUI.StartCoroutine(hpUI.HideHealthGroups()); 
    }

    void UpdateUIWithSpecificHP(int p1, int p2) { if (hpUI == null) return; int idx = (Runner.IsServer) ? 0 : 1; if (idx == 0) hpUI.UpdateHealthUI(p1, p2); else hpUI.UpdateHealthUI(p2, p1); }
    public bool IsMyTurn() { if (Runner == null || Runner.LocalPlayer == PlayerRef.None) return false; int idx = (Runner.IsServer) ? 0 : 1; return idx == activePlayerIndex; }
    public bool GetCurrentBulletStatus() {  return bulletCount > 0 && bullets[0]; }
    void CheckGameOver() { if (player1HP <= 0) Debug.Log("PLAYER 2 WIN"); if (player2HP <= 0) Debug.Log("PLAYER 1 WIN"); }

    private void OnDestroy() {
        DOTween.KillAll(); // Dọn dẹp Tween khi đổi scene
    }
}