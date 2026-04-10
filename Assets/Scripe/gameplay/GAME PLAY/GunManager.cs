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
    [Networked] public NetworkBool hasShotThisTurn { get; set; } 
    [Networked] public float finalGunAngle { get; set; }
    
    // MỚI: Biến khóa toàn cục khi đang diễn animation (Dùng cho Items)
    [Networked] public NetworkBool isAnimatingAction { get; set; }

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

    // --- LOCAL CONTROL ---
    private bool localActionLock = false; 
    private bool isShotCanvasVisible = false;
    private bool isWaitingTextVisible = false;
    private Coroutine _healthAnimCoroutine;

    [Header("Mouse Sway Settings")]
    public float swayAmount = 2.0f; 
    public float swaySmoothing = 5.0f; 
    private Quaternion baseRotation; 
    private bool canSway = false; 
    private float breatheTimer; 

    public override void Spawned()
    {
        if (roundPanel != null) roundCanvasGroup = roundPanel.GetComponent<CanvasGroup>();
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
            isAnimatingAction = false; // Khởi tạo rảnh
            StartCoroutine(MasterStartSequence());
        }
    }

    // --- LOGIC KIỂM TRA QUYỀN TƯƠNG TÁC ---
    public bool CanIInteract()
    {
        // Chặn tương tác nếu đang đợi round, đã bắn, bị khóa local hoặc ĐANG DIỄN ANIMATION vật phẩm
        return IsMyTurn() && !hasShotThisTurn && !isWaitingNextRound && !localActionLock && !isAnimatingAction;
    }

    public void RequestShoot(bool shootSelf) 
    { 
        if (CanIInteract() && (hpUI == null || !hpUI.isAnimating)) 
        {
            localActionLock = true; 
            RPC_Shoot(shootSelf); 
        } 
    }

    // Thêm hàm này vào GunManager.cs
    public void ResetActionLock()
    {
        // 1. Nhả khóa mạng (Chỉ máy chủ/máy bắn mới có quyền ghi)
        if (HasStateAuthority) isAnimatingAction = false;
        
        // 2. Nhả khóa tại máy đang chơi (Quan trọng nhất để hiện nút)
        localActionLock = false;
        hasShotThisTurn = false; // Đảm bảo lượt mới không bị tính là đã bắn

        // 3. Ép thanh máu dừng báo cáo bận
        if (hpUI != null) hpUI.isAnimating = false; 
        
        Debug.Log("<color=green>[GunManager]</color> Đã cưỡng ép RESET toàn bộ khóa UI!");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
    public void RPC_Shoot(bool shootSelf) { 
        if (bulletCount <= 0 || isWaitingNextRound || hasShotThisTurn || isAnimatingAction) return; 
        
        hasShotThisTurn = true; 
        bool isReal = bullets[0]; 
        bool isLast = (bulletCount == 1); 
        
        for (int i = 0; i < bulletCount - 1; i++) bullets.Set(i, bullets[i + 1]); 
        bulletCount--; 
        
        int dmg = doubleDamage ? 2 : 1; 
        doubleDamage = false; 
        
        bool shouldChangeTurn = true; 
        if (isReal) { 
            if (activePlayerIndex == 0) { if (shootSelf) player1HP -= dmg; else player2HP -= dmg; } 
            else { if (shootSelf) player2HP -= dmg; else player1HP -= dmg; }
            shouldChangeTurn = true; 
        } else {
            shouldChangeTurn = !shootSelf; 
        }
        
        player1HP = Mathf.Max(0, player1HP); 
        player2HP = Mathf.Max(0, player2HP); 

        RPC_AnimateHealth(player1HP, player2HP, shouldChangeTurn, isLast); 

        if (shouldChangeTurn) {
            ChangeTurn(); 
        } else if (!isLast) {
            hasShotThisTurn = false; 
        }

        if (isLast) {
            isWaitingNextRound = true;
            if (player1HP > 0 && player2HP > 0) { 
                currentRound++; 
                StartCoroutine(WaitForHealthThenRound()); 
            }
        }
        CheckGameOver(); 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] 
    public void RPC_AnimateHealth(int p1, int p2, bool turnedChanged, bool lastBullet) { 
        if (_healthAnimCoroutine != null) StopCoroutine(_healthAnimCoroutine);
        _healthAnimCoroutine = StartCoroutine(HealthAnimationSequence(p1, p2, turnedChanged, lastBullet)); 
    }

    IEnumerator HealthAnimationSequence(int p1, int p2, bool turnedChanged, bool lastBullet) { 
        if (hpUI != null) {
            UpdateUIWithSpecificHP(p1, p2); 
            yield return StartCoroutine(hpUI.ShowHealthGroups()); 
            yield return new WaitForSeconds(1.5f); // Thời gian chờ xem máu trừ
            yield return StartCoroutine(hpUI.HideHealthGroups()); 
        }

        // --- ĐOẠN SỬA: Phải gọi ResetActionLock ở đây ---
        ResetActionLock();

        // Nếu là viên cuối, không nhả lock ngay mà để NextRoundRoutine xử lý
        if (lastBullet) {
            isWaitingNextRound = true;
        }
    }

    public void ChangeTurn() { 
        if (!HasStateAuthority) return;
        hasShotThisTurn = false; 
        if (isCuffed) { 
            isCuffed = false; 
            return; 
        } 
        activePlayerIndex = (activePlayerIndex == 0) ? 1 : 0; 
        RPC_SyncVisuals();
    }

    // --- CÁC HÀM KHỞI TẠO VÀ HIỆU ỨNG ---
    IEnumerator MasterStartSequence()
    {
        while (!canStartSequence) yield return null;
        isWaitingNextRound = true; 

        if (HasStateAuthority) 
        {
            finalGunAngle = 3600f + Random.Range(0f, 360f); 
            RPC_StartGunSpin(finalGunAngle); 
        }
        
        yield return new WaitForSeconds(11.5f); 

        if (HasStateAuthority) 
        {
            RPC_TriggerHealthIntro(); 
            yield return new WaitForSeconds(1.5f); 
            RPC_PlayRoundEffect(currentRound);
            yield return new WaitForSeconds(roundDisplayDuration); 
            StartCoroutine(NextRoundRoutine(true)); 
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_StartGunSpin(float syncAngle) { StartCoroutine(GunSpinRoutine(syncAngle)); }

    IEnumerator GunSpinRoutine(float targetTotalZ)
    {
        Camera currentActiveCam = Camera.main; 
        if (rotatingGun != null && currentActiveCam != null)
        {
            canSway = false; 
            currentActiveCam.transform.DOKill(); 
            Quaternion originalRotation = currentActiveCam.transform.rotation;
            rotatingGun.transform.DOKill();

            currentActiveCam.transform.DOLookAt(rotatingGun.transform.position, zoomDuration).SetEase(Ease.InOutSine);
            currentActiveCam.DOFieldOfView(zoomFOV, zoomDuration).SetEase(Ease.InOutSine);
            
            float currentZ = rotatingGun.transform.localEulerAngles.z;
            DOTween.To(() => currentZ, x => currentZ = x, targetTotalZ, 10f)
                .SetEase(Ease.OutQuart)
                .OnUpdate(() => { rotatingGun.transform.localRotation = Quaternion.Euler(90f, 0f, currentZ); });

            yield return new WaitForSeconds(10.5f);

            float finalNormalizedZ = targetTotalZ % 360f;
            if (finalNormalizedZ < 0) finalNormalizedZ += 360f;
            int pointingAtPlayer = (finalNormalizedZ > 180f) ? 0 : 1;

            if (targetText != null)
            {
                int myLocalIndex = (Runner.IsServer) ? 0 : 1;
                targetText.text = (pointingAtPlayer == myLocalIndex) ? "BẠN ĐI TRƯỚC" : "HỌ ĐI TRƯỚC";
                targetText.color = (pointingAtPlayer == myLocalIndex) ? Color.green : Color.red;
                targetText.gameObject.SetActive(true);
                targetText.transform.localScale = Vector3.zero;
                targetText.transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack).OnComplete(() => {
                    targetText.transform.DOScale(0f, 0.3f).SetDelay(1f).OnComplete(() => targetText.gameObject.SetActive(false));
                });
            }

            rotatingGun.transform.DOShakeRotation(0.5f, new Vector3(0, 0, 10), 10, 90);
            currentActiveCam.DOFieldOfView(normalFOV, 1f).SetEase(Ease.OutBack);
            yield return currentActiveCam.transform.DORotateQuaternion(originalRotation, 1f).SetEase(Ease.OutExpo).WaitForCompletion(); 

            baseRotation = originalRotation; 
            breatheTimer = 0f; 
            canSway = true; 
            yield return new WaitForSeconds(0.8f);

            if (HasStateAuthority) RPC_FinalizeWinner(pointingAtPlayer);
        }
    }

    public IEnumerator NextRoundRoutine(bool isFirstRound = false) {
        if (!HasStateAuthority) yield break;
        isWaitingNextRound = true; 
        hasShotThisTurn = false;
        GenerateBullets();

        if (!isFirstRound) {
            yield return new WaitForSeconds(1f);
            RPC_PlayRoundEffect(currentRound);
            yield return new WaitForSeconds(roundDisplayDuration + 0.5f); 
        }
        else yield return new WaitForSeconds(0.5f);

        int itemsToGive = (currentRound == 1) ? 0 : Mathf.Min(currentRound - 1, 4);

        if (itemsManager != null && itemsToGive > 0) {
            itemsManager.GiveRandomItemsToBoth(itemsToGive);
            yield return new WaitForSeconds(1.5f); 
        }

        RPC_SyncVisuals();
        isWaitingNextRound = false; 
    }

    void Update() 
    {
        // 1. Kiểm tra an toàn
        if (Object == null || Runner == null) return;
        
        // 2. Định nghĩa trạng thái "Rảnh": Không đợi round, không diễn Anim, không nhảy máu
        bool isGameIdle = canStartSequence && !isWaitingNextRound && !isAnimatingAction && (hpUI == null || !hpUI.isAnimating);
        bool isMyTurn = IsMyTurn();

        // --- LOGIC HIỆN NÚT BẮN (Bản thân & Đối phương) ---
        bool shouldShowShot = isGameIdle && isMyTurn && !hasShotThisTurn && !localActionLock;
        
        if (shouldShowShot != isShotCanvasVisible) 
        {
            isShotCanvasVisible = shouldShowShot;
            shotCanvas.transform.DOKill();
            if (shouldShowShot) 
            {
                shotCanvas.SetActive(true);
                shotCanvas.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
                Debug.Log("<color=green>[UI]</color> Đã hiện nút bắn!");
            } 
            else 
            {
                shotCanvas.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => shotCanvas.SetActive(false));
                Debug.Log("<color=yellow>[UI]</color> Đã ẩn nút bắn!");
            }
        }

        // --- LOGIC HIỆN DÒNG CHỮ "ĐỢI ĐỐI THỦ" ---
        bool shouldShowWaiting = isGameIdle && !isMyTurn;
        if (shouldShowWaiting != isWaitingTextVisible) 
        {
            isWaitingTextVisible = shouldShowWaiting;
            waitingText.transform.DOKill();
            if (shouldShowWaiting) 
            {
                waitingText.gameObject.SetActive(true);
                waitingText.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            } 
            else 
            {
                waitingText.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => waitingText.gameObject.SetActive(false));
            }
        }

        // --- DEBUG LOG (Để ông soi lỗi khi nút không hiện) ---
// --- DEBUG LOG (Bản sửa lỗi chạy vô hạn) ---
        if (isMyTurn && !hasShotThisTurn && !shouldShowShot && canStartSequence && !isWaitingNextRound)
        {
            string reason = "";
            if (isAnimatingAction) reason += "AnimAction ";
            if (hpUI != null && hpUI.isAnimating) reason += "AnimHP ";
            if (localActionLock) reason += "LocalLock ";
            
            // CHỈ LOG KHI CÓ SỰ THAY ĐỔI: Dùng Time.frameCount để chỉ log 1 giây 1 lần cho đỡ lag
            if (reason != "" && Time.frameCount % 60 == 0) 
            {
                Debug.LogWarning($"<color=red>[UI Lock]</color> Đợi nhả: {reason}");
            }
        }

        // --- HIỆU ỨNG CAMERA SWAY (Giữ nguyên) ---
        if (canSway && Camera.main != null)
        {
            breatheTimer += Time.deltaTime;
            float breatheOffset = Mathf.Sin(breatheTimer * 1.2f) * 1f;
            float mouseX = (Input.mousePosition.x / Screen.width) * 2 - 1;
            float mouseY = (Input.mousePosition.y / Screen.height) * 2 - 1;
            Quaternion targetSway = Quaternion.Euler((-mouseY * swayAmount) + breatheOffset, mouseX * swayAmount, 0);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, baseRotation * targetSway, Time.deltaTime * swaySmoothing);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] void RPC_FinalizeWinner(int winnerIdx) { activePlayerIndex = winnerIdx; RotateGunToActivePlayer(); }
    void RotateGunToActivePlayer() { if (rotatingGun == null) return; rotatingGun.transform.DOKill(); float targetZ = (activePlayerIndex == 0) ? -90f : 90f; rotatingGun.transform.DORotate(new Vector3(90f, 0f, targetZ), 0.8f).SetEase(Ease.OutBack); }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] void RPC_TriggerHealthIntro() { if (hpUI != null) hpUI.StartHealthIntro(); }
    
    void GenerateBullets() { 
        if (!HasStateAuthority) return; 
        List<bool> temp = new List<bool>(); 
        if (currentRound == 1) AddBulletsToList(temp, 1, 1); 
        else AddBulletsToList(temp, Random.Range(2, 5), Random.Range(2, 5)); 
        for (int i = 0; i < temp.Count; i++) { int r = Random.Range(i, temp.Count); (temp[i], temp[r]) = (temp[r], temp[i]); } 
        for (int i = 0; i < temp.Count; i++) bullets.Set(i, temp[i]); 
        bulletCount = temp.Count; 
    }

    void AddBulletsToList(List<bool> list, int real, int blank) { for (int i = 0; i < real; i++) list.Add(true); for (int i = 0; i < blank; i++) list.Add(false); }
    IEnumerator WaitForHealthThenRound() { yield return new WaitForSeconds(4.0f); StartCoroutine(NextRoundRoutine()); }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] void RPC_SyncVisuals() => RotateGunToActivePlayer();
    
    void UpdateUIWithSpecificHP(int p1, int p2) { 
        if (hpUI == null) return; 
        int idx = (Runner.IsServer) ? 0 : 1; 
        if (idx == 0) hpUI.UpdateHealthUI(p1, p2); else hpUI.UpdateHealthUI(p2, p1); 
    }

    public bool IsMyTurn() { if (Runner == null || Runner.LocalPlayer == PlayerRef.None) return false; int idx = (Runner.IsServer) ? 0 : 1; return idx == activePlayerIndex; }
    public bool GetCurrentBulletStatus() { return bulletCount > 0 && bullets[0]; }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] public void RPC_PlayRoundEffect(int roundNumber) {
        if (roundPanel == null || roundCanvasGroup == null) return;
        roundText.text = "ROUND " + roundNumber; roundPanel.SetActive(true); roundCanvasGroup.alpha = 0f; 
        roundCanvasGroup.DOFade(1f, 0.5f).OnComplete(() => { roundCanvasGroup.DOFade(0f, 0.5f).SetDelay(roundDisplayDuration).OnComplete(() => roundPanel.SetActive(false)); });
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)] public void RPC_ShowGlassResult([RpcTarget] PlayerRef target, bool isReal) { Debug.Log($"<color={(isReal ? "red" : "white")}>[SOI ĐẠN] Kết quả: {(isReal ? "ĐẠN THẬT" : "ĐẠN GIẢ")}</color>"); }
    void CheckGameOver() { if (player1HP <= 0) Debug.Log("PLAYER 2 WIN"); if (player2HP <= 0) Debug.Log("PLAYER 1 WIN"); }
    private void OnDestroy() { DOTween.KillAll(); }
}