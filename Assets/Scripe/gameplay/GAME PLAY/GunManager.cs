using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using DG.Tweening;
using System.Linq;

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
    [Networked] public NetworkBool isAnimatingAction { get; set; }
    [Networked] public int cuffedPlayerIndex { get; set; } = -1;

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

    [Header("Bullet Visual Effects")]
    public GameObject realBulletPrefab;  // Prefab viên đạn thật
    public GameObject blankBulletPrefab; // Prefab viên đạn giả
    public Transform bulletSpawnPoint;   // Điểm trên cao để đạn rơi xuống
    public Transform bulletTablePoint;   // Điểm tập kết trên bàn (có thể là một hàng ngang)

    // --- LOCAL CONTROL ---
    private bool localActionLock = false; 
    private bool isShotCanvasVisible = false;
    private bool isWaitingTextVisible = false;
    private Coroutine _healthAnimCoroutine;
    private bool isStartingNextRound = false;
    private bool isGameOver = false;
    [Header("Mouse Sway Settings")]
    public float swayAmount = 2.0f; 
    public float swaySmoothing = 5.0f; 
    private Quaternion baseRotation; 
    private bool canSway = false; 
    private float breatheTimer; 

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultText;

    public void UnlockLocalAction()
    {
        localActionLock = false;
    }

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

private IEnumerator AnimateBulletSequence(List<bool> bulletList)
{
    List<GameObject> spawnedBullets = new List<GameObject>();

    // --- CẤU HÌNH KHOẢNG CÁCH ---
    float spacing = 0.1f; // 🔥 CHỈNH SỐ NÀY: Càng nhỏ đạn càng sát nhau (thử 0.12 nếu vẫn thấy thưa)
    
    // Tính toán để hàng đạn luôn nằm CHÍNH GIỮA TablePoint
    float totalWidth = (bulletList.Count - 1) * spacing;
    Vector3 startPos = bulletTablePoint.position - new Vector3(totalWidth / 2f, 0, 0);

    // 1. Rơi từng viên xuống bàn
    for (int i = 0; i < bulletList.Count; i++)
    {
        GameObject prefab = bulletList[i] ? realBulletPrefab : blankBulletPrefab;
        // Sinh ra đạn
        GameObject b = Instantiate(prefab, bulletSpawnPoint.position, Quaternion.identity);
        spawnedBullets.Add(b);

        // Vị trí mục tiêu trên bàn
        Vector3 targetPos = startPos + new Vector3(i * spacing, 0, 0); 
        
        // Hiệu ứng rơi (tăng thời gian rơi lên 0.6s cho mượt)
        b.transform.DOMove(targetPos, 0.6f).SetEase(Ease.OutBounce);
        
        // 🔥 CHỜ ĐỌC ĐẠN: Đợi lâu hơn một chút giữa mỗi viên rơi xuống
        yield return new WaitForSeconds(0.4f); 
    }

    // 2. 🔥 QUAN TRỌNG: THỜI GIAN QUAN SÁT
    // Tăng lên 2 giây hoặc hơn để người chơi kịp đếm màu đạn
    Debug.Log("<color=white>Đang cho người chơi quan sát đạn...</color>");
    yield return new WaitForSeconds(2.0f); 

    // 3. Tất cả bay vào súng
    foreach (var b in spawnedBullets)
    {
        if (b != null)
        {
            // Cho đạn bay vào súng chậm lại một chút (0.6s) để thấy rõ hành động nạp
            b.transform.DOMove(rotatingGun.transform.position, 0.6f).SetEase(Ease.InBack);
            b.transform.DOScale(Vector3.zero, 0.6f).OnComplete(() => Destroy(b));
        }
    }

    // Đợi hiệu ứng nạp đạn hoàn tất trước khi hiện chữ Round
    yield return new WaitForSeconds(0.8f);
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

[Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
public void RPC_Shoot(bool shootSelf) 
{ 
    if (bulletCount <= 0 || isWaitingNextRound || hasShotThisTurn || isAnimatingAction) return; 
    
    // Tìm Player đang thực hiện lượt này
    var allPlayers = FindObjectsByType<PlayerActionController>(FindObjectsSortMode.None);
    var actingPlayer = allPlayers.FirstOrDefault(p => p.PlayerIndex == activePlayerIndex);

    if (actingPlayer != null)
    {
        // GỌI Ở ĐÂY: Ra lệnh cho nhân vật bắt đầu diễn kịch bản nhặt súng và bắn
        actingPlayer.RPC_StartShootingSequence(shootSelf);
    }

    hasShotThisTurn = true; 
    bool isReal = bullets[0]; 
    bool isLast = (bulletCount == 1); 
    
    EjectBullet(); 
    
    int dmg = doubleDamage ? 2 : 1; 
    doubleDamage = false; 
    
    bool shouldChangeTurn = true; 

    if (isReal) 
    { 
        // --- LOGIC TÍNH SÁT THƯƠNG CHUẨN ---
        if (activePlayerIndex == 0) // Lượt của Player 1 (Bên trái)
        {
            if (shootSelf) 
                player1HP -= dmg; // P1 tự bắn mình -> P1 mất máu
            else 
                player2HP -= dmg; // P1 bắn đối thủ -> P2 mất máu
        } 
        else // Lượt của Player 2 (Bên phải)
        {
            if (shootSelf) 
                player2HP -= dmg; // P2 tự bắn mình -> P2 mất máu
            else 
                player1HP -= dmg; // P2 bắn đối thủ -> P1 mất máu
        }
        shouldChangeTurn = true; 
    } 
    else // Đạn giả (Blank)
    {
        // Nếu tự bắn mình bằng đạn giả thì được giữ lượt (không đổi turn)
        shouldChangeTurn = !shootSelf; 
    }
    
    // Đảm bảo máu không bao giờ xuống dưới 0
    player1HP = Mathf.Max(0, player1HP); 
    player2HP = Mathf.Max(0, player2HP); 

    if (isLast && player1HP > 0 && player2HP > 0) 
    {
        isWaitingNextRound = true; 
    }

    // Gửi dữ liệu máu đã cập nhật xuống các máy để diễn hiệu ứng
    RPC_AnimateHealth(player1HP, player2HP, shouldChangeTurn, isLast); 

    if (shouldChangeTurn) 
    {
        ChangeTurn(); 
    }
    else 
    {
        hasShotThisTurn = false; 
        RPC_UnlockLocalForAll(); // Mở khóa cho phép bắn tiếp vì được giữ lượt
    }

    CheckGameOver(); 
}
    // Thêm hàm phụ này ngay dưới RPC_Shoot để hỗ trợ Invoke
    private void CallNextRoundRoutine() 
    {
        if (HasStateAuthority)
        {
            StartCoroutine(NextRoundRoutine(false));
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] 
    public void RPC_AnimateHealth(int p1, int p2, bool turnedChanged, bool lastBullet) { 
        if (_healthAnimCoroutine != null) StopCoroutine(_healthAnimCoroutine);
        _healthAnimCoroutine = StartCoroutine(HealthAnimationSequence(p1, p2, turnedChanged, lastBullet)); 
    }

    IEnumerator HealthAnimationSequence(int p1, int p2, bool turnedChanged, bool lastBullet) { 
        if (hpUI != null) {

            hpUI.UpdateHealthUI(p1, p2);

            yield return StartCoroutine(hpUI.ShowHealthGroups()); 
            yield return new WaitForSeconds(1.0f); 
            yield return StartCoroutine(hpUI.HideHealthGroups()); 
        }

        // NẾU LÀ VIÊN CUỐI: Server sẽ chủ động tăng Round và nạp đạn
        if (lastBullet && HasStateAuthority && p1 > 0 && p2 > 0) 
        {
            // Tăng Round ngay tại đây để NextRoundRoutine đọc được giá trị mới
            currentRound++; 
            Debug.Log($"<color=orange>[Logic]</color> Chuyển sang Round {currentRound}");
            StartCoroutine(NextRoundRoutine(false));
        }
    }

public void ChangeTurn() 
{ 
    if (!HasStateAuthority) return;

    hasShotThisTurn = false; 

    // 🔥 Đổi sang player tiếp theo trước
    int nextPlayer = (activePlayerIndex == 0) ? 1 : 0;

    // 🔥 Nếu có còng
    if (isCuffed && cuffedPlayerIndex == nextPlayer)
    {
        Debug.Log("<color=yellow>[CUFF]</color> Player " + nextPlayer + " bị còng → mất lượt");

        // ❌ Skip thằng bị còng
        nextPlayer = (nextPlayer == 0) ? 1 : 0;

        // ❌ Xóa hiệu lực còng (chỉ 1 lượt)
        isCuffed = false;
        cuffedPlayerIndex = -1;
    }

    // 🔥 Gán lượt mới
    activePlayerIndex = nextPlayer;

    // 🔥 Mở khóa input cho tất cả client
    RPC_UnlockLocalForAll();

    // 🔥 Sync UI + animation
    RPC_SyncVisuals();
}

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncVisuals()
    {
        RotateGunToActivePlayer();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_UnlockLocalForAll()
    {
        localActionLock = false;
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

public IEnumerator NextRoundRoutine(bool isFirstRound = false) 
{
    // CỰC KỲ QUAN TRỌNG: Chỉ Server mới chạy logic tính toán
    if (!HasStateAuthority) yield break;

    isWaitingNextRound = true; 
    hasShotThisTurn = false;

    // 1. DỌN RÁC (Chỉ dọn trên Server, RPC sẽ dọn trên Client)
    RPC_ClearOldBullets();
    yield return new WaitForSeconds(0.2f);

    // 2. TÍNH TOÁN SỐ ĐẠN (Host quyết định)
    int tReal = 0; int tBlank = 0; int itemsToGive = 0;
    if (currentRound == 1) { tReal = 1; tBlank = 2; itemsToGive = 0; }
    else if (currentRound == 2) { tReal = 2; tBlank = 2; itemsToGive = 2; }
    else {
        int total = Mathf.Clamp(3 + (currentRound / 2), 4, 8);
        tReal = Random.Range(2, total);
        tBlank = total - tReal;
        itemsToGive = 2;
    }

    // 3. CẬP NHẬT DỮ LIỆU MẠNG ĐỂ BẮN (Xào đạn bí mật)
    List<bool> shootList = new List<bool>();
    for(int i = 0; i < tReal; i++) shootList.Add(true);
    for(int i = 0; i < tBlank; i++) shootList.Add(false);

    for (int i = 0; i < shootList.Count; i++) {
        int r = Random.Range(i, shootList.Count);
        (shootList[i], shootList[r]) = (shootList[r], shootList[i]);
    }

    bulletCount = shootList.Count;
    for (int i = 0; i < bulletCount; i++) bullets.Set(i, shootList[i]);

    // 4. HIỆN CHỮ ROUND (Cho cả 2 máy)
    RPC_PlayRoundEffect(currentRound);
    yield return new WaitForSeconds(1.0f); 

    // 5. 🔥 PHÁT LỆNH DIỄN HIỆU ỨNG CHO CẢ 2 MÁY (Quan trọng nhất)
    // Truyền tReal và tổng số đạn để máy khách tự tạo danh sách hiển thị Đỏ/Trắng
    // Tìm trong hàm NextRoundRoutine (khoảng dòng 265), sửa lại thành:
    RPC_AnimateBulletsForAll(tReal, bulletCount);

    // Đợi thời gian diễn hoạt đạn (tầm 3-4s tùy độ dài hàm Animate của ông)
    yield return new WaitForSeconds(4.0f); 

    // 6. PHÁT ĐỒ
    if (itemsManager != null && itemsToGive > 0)
    {
        itemsManager.GiveRandomItemsToBoth(itemsToGive);
        yield return new WaitForSeconds(1.5f);
    }

    // 7. GIẢI PHÓNG
    isWaitingNextRound = false; 
    RPC_UnlockLocalForAll();
}

// --- CÁC RPC HỖ TRỢ ĐỂ ĐỒNG BỘ HIỂN THỊ ---

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_ClearOldBullets()
{
    GameObject[] oldBullets = GameObject.FindGameObjectsWithTag("Bullet");
    foreach (var b in oldBullets) {
        if (b != null) {
            b.transform.DOKill();
            Destroy(b);
        }
    }
}

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_AnimateBulletsForAll(int realCount, int totalCount)
{
    // Mỗi máy tự tạo một list hiển thị (Thật hiện trước cho dễ đếm)
    List<bool> displayList = new List<bool>();
    for(int i = 0; i < realCount; i++) displayList.Add(true);
    for(int i = 0; i < (totalCount - realCount); i++) displayList.Add(false);

    // Mỗi máy tự chạy Coroutine diễn hoạt cục bộ của mình
    StartCoroutine(AnimateBulletSequence(displayList));
}

// --- SỬA LỖI VĂNG 2 VIÊN SODA ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AnimateSodaEject(bool isReal)
    {
        // Chặn nếu lệnh thực thi quá nhanh (chống văng 2 viên trên máy Host)
        if (Time.time - _lastEjectTime < 0.1f) return;
        _lastEjectTime = Time.time;

        if (muzzlePoint == null) return;

        GameObject prefab = isReal ? realBulletPrefab : blankBulletPrefab;
        GameObject ejectedBullet = Instantiate(prefab, muzzlePoint.position, muzzlePoint.rotation);
        ejectedBullet.tag = "Bullet"; 

        Vector3 dropPos = bulletTablePoint.position + new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));
        ejectedBullet.transform.DOJump(dropPos, 0.5f, 1, 0.6f).SetEase(Ease.OutQuad);
        ejectedBullet.transform.DORotate(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), 0.6f);

        ejectedBullet.transform.DOScale(Vector3.zero, 0.3f).SetDelay(1.2f).OnComplete(() => {
            if(ejectedBullet != null) Destroy(ejectedBullet);
        });
    }
    void Update() 
    {
         if (isGameOver) return; // 🔥 CHẶN HẾT UI + logic
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
    


    public bool IsMyTurn() { if (Runner == null || Runner.LocalPlayer == PlayerRef.None) return false; int idx = (Runner.IsServer) ? 0 : 1; return idx == activePlayerIndex; }
    // Đổi tên từ GetCurrentBulletStatus thành GetCurrentBulletType để khớp với các script khác
    public bool GetCurrentBulletType() 
    { 
        return bulletCount > 0 && bullets[0]; 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] public void RPC_PlayRoundEffect(int roundNumber) {
        if (roundPanel == null || roundCanvasGroup == null) return;
        roundText.text = "ROUND " + roundNumber; roundPanel.SetActive(true); roundCanvasGroup.alpha = 0f; 
        roundCanvasGroup.DOFade(1f, 0.5f).OnComplete(() => { roundCanvasGroup.DOFade(0f, 0.5f).SetDelay(roundDisplayDuration).OnComplete(() => roundPanel.SetActive(false)); });
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)] public void RPC_ShowGlassResult([RpcTarget] PlayerRef target, bool isReal) { Debug.Log($"<color={(isReal ? "red" : "white")}>[SOI ĐẠN] Kết quả: {(isReal ? "ĐẠN THẬT" : "ĐẠN GIẢ")}</color>"); }
    void CheckGameOver() 
    {
        if (!HasStateAuthority || isGameOver) return;

        if (player1HP <= 0 || player2HP <= 0)
        {
            isGameOver = true;

            // 🔥 CHẶN TOÀN BỘ GAME
            isWaitingNextRound = true;
            hasShotThisTurn = true;
            localActionLock = true;

            int winner = (player1HP <= 0) ? 1 : 0;
            RPC_ShowGameOver(winner);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_ShowGameOver(int winnerIndex)
    {
        if (gameOverPanel == null || resultText == null) return;

        gameOverPanel.SetActive(true);

        int myIndex = (Runner.IsServer) ? 0 : 1;

        if (myIndex == winnerIndex)
        {
            resultText.text = "BẠN THẮNG";
            resultText.color = Color.green;
        }
        else
        {
            resultText.text = "BẠN THUA";
            resultText.color = Color.red;
        }
    }
    private void OnDestroy() { DOTween.KillAll(); }

    public void EjectBullet() 
    {
        if (bulletCount <= 0) return;

        // Tạo một list tạm để tính toán logic dồn đạn
        List<bool> tempBullets = new List<bool>();
        for (int i = 1; i < bulletCount; i++) {
            tempBullets.Add(bullets[i]);
        }

        // Cập nhật lại NetworkArray cho tất cả các máy cùng thấy
        for (int i = 0; i < tempBullets.Count; i++) {
            bullets.Set(i, tempBullets[i]);
        }

        bulletCount--;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ExitGame()
    {
        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (Runner != null)
        {
            Runner.Shutdown(); // 🔥 tắt network đúng cách
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("menu"); // nhớ đổi tên scene
    }

    public void OnExitButton()
    {
        if (Runner.IsServer)
        {
            RPC_ExitGame(); // host → gọi cho tất cả
        }
        else
        {
            StartCoroutine(ExitRoutine()); // client → tự thoát
        }
    }
}