using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TMPro;
using DG.Tweening;

public class GunManager : NetworkBehaviour
{
    [Header("New References")]
    public Transform muzzlePoint; 

    [Header("Cinematic Settings")]
    public float zoomFOV = 30f;    
    public float normalFOV = 60f;  
    public float zoomDuration = 1.5f;
    public Transform p1TablePos; 
    public Transform p2TablePos; 

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

    [Header("References")]
    public ItemsManager itemsManager; 
    public GameObject rotatingGun; 

    [Header("Settings")]
    public int maxHP = 5;
    public GameObject shotCanvas; 
    
    [Header("UI Round Settings")]
    public GameObject roundPanel; 
    public TextMeshProUGUI roundText;
    private CanvasGroup roundCanvasGroup;

    [Header("Health UI Settings")]
    public HealthBarController hpUI; 

    public override void Spawned()
    {
        if (roundPanel != null) 
            roundCanvasGroup = roundPanel.GetComponent<CanvasGroup>();

        if (HasStateAuthority)
        {
            player1HP = maxHP;
            player2HP = maxHP;
            currentRound = 1;
            doubleDamage = false;
            isCuffed = false;

            // Bắt đầu chuỗi logic chọn người đi trước
            StartCoroutine(MasterStartSequence());
        }
    }

    IEnumerator MasterStartSequence()
    {
        isWaitingNextRound = true; 
        RPC_StartGunSpin();

        // Tổng thời gian đợi: Quay 10s + Rung 0.5s + Di chuyển 1.2s + Buffer
        yield return new WaitForSeconds(12.5f);
        yield return new WaitForSeconds(2.0f);

        if (HasStateAuthority) 
        {
            // Bây giờ súng đã nằm yên trên bàn, mới bắt đầu Round 1 và tặng đồ
            StartCoroutine(NextRoundRoutine());
            if (hpUI != null) RPC_TriggerHealthIntro();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_StartGunSpin() { StartCoroutine(GunSpinRoutine()); }

    IEnumerator GunSpinRoutine()
    {
        Camera currentActiveCam = Camera.main; 
        if (rotatingGun != null)
        {
            rotatingGun.transform.DOKill();
            if (currentActiveCam != null) 
                currentActiveCam.DOFieldOfView(zoomFOV, zoomDuration).SetEase(Ease.InOutSine);
            
            float startY = rotatingGun.transform.localEulerAngles.y;
            float randomExtraAngle = Random.Range(0f, 360f);
            float targetTotalY = startY + 3600f + randomExtraAngle; 

            float currentY = startY;
            DOTween.To(() => currentY, x => currentY = x, targetTotalY, 10f)
                .SetEase(Ease.OutQuart)
                .OnUpdate(() => {
                    rotatingGun.transform.localRotation = Quaternion.Euler(90f, currentY, 0f);
                });

            yield return new WaitForSeconds(10.5f);
            
            rotatingGun.transform.DOShakeRotation(0.5f, new Vector3(0, 10, 0), 10, 90);
            
            if (currentActiveCam != null)
                currentActiveCam.DOFieldOfView(normalFOV, 1f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(0.5f);

            float finalY = rotatingGun.transform.localEulerAngles.y % 360f;
            if (finalY < 0) finalY += 360f;

            int winner = (finalY > 90f && finalY <= 270f) ? 1 : 0;

            if (HasStateAuthority)
            {
                activePlayerIndex = winner;
                float snapY = (winner == 0) ? 0f : 180f;
                RPC_FinalizeWinner(winner, snapY);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_FinalizeWinner(int winnerIdx, float snapY)
    {
        activePlayerIndex = winnerIdx;
        MoveGunToActivePlayerTable(snapY);
    }

    void MoveGunToActivePlayerTable(float forcedY = -1f)
    {
        if (rotatingGun == null) return;
        rotatingGun.transform.DOKill();

        Transform targetPos = (activePlayerIndex == 0) ? p1TablePos : p2TablePos;
        
        if (targetPos != null)
        {
            rotatingGun.transform.DOMove(targetPos.position, 1.2f).SetEase(Ease.OutBack);
            float targetY = (forcedY != -1f) ? forcedY : targetPos.eulerAngles.y;
            rotatingGun.transform.DORotate(new Vector3(90f, targetY, 0f), 1.2f).SetEase(Ease.OutBack);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_TriggerHealthIntro() { if (hpUI != null) hpUI.StartHealthIntro(); }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SyncGunPosition() => MoveGunToActivePlayerTable();

    public IEnumerator NextRoundRoutine() {
        if (!HasStateAuthority) yield break;

        // 1. Khóa mọi hành động, chuẩn bị đạn
        isWaitingNextRound = true; 
        GenerateBullets();

        // 2. Hiện hiệu ứng ROUND (Chữ Round hiện ra và biến mất)
        yield return new WaitForSeconds(0.5f);
        RPC_PlayRoundEffect(currentRound);
        
        // Đợi hiệu ứng chữ Round chạy xong (khoảng 2.5s theo code RPC_PlayRoundEffect của ông)
        yield return new WaitForSeconds(2.5f); 

        // 3. Hiện đồ dần dần
        int itemsToGive = (currentRound == 1) ? 2 : Mathf.Min(currentRound, 4);
        if (itemsManager != null && itemsToGive > 0) 
        {
            // Gọi lệnh tặng đồ
            itemsManager.GiveRandomItemsToBoth(itemsToGive);
            
            // Đợi một chút để đồ "rơi" xong mới hiện nút bắn
            yield return new WaitForSeconds(1.0f); 
        }

        // 4. Mở khóa và hiện nút bắn (shotCanvas)
        isWaitingNextRound = false; 
        // Khi isWaitingNextRound = false, hàm Update sẽ tự động bật shotCanvas lên
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayRoundEffect(int roundNumber) {
        if (roundPanel == null || roundCanvasGroup == null) return;
        roundText.text = "ROUND " + roundNumber; roundPanel.SetActive(true);
        roundCanvasGroup.alpha = 0f; roundCanvasGroup.DOFade(1f, 0.5f).OnComplete(() => {
            roundCanvasGroup.DOFade(0f, 0.5f).SetDelay(1.5f).OnComplete(() => roundPanel.SetActive(false));
        });
    }

    void GenerateBullets() {
        if (!HasStateAuthority) return; 
        List<bool> tempBullets = new List<bool>();
        if (currentRound == 1) AddBulletsToList(tempBullets, 1, 1);
        else AddBulletsToList(tempBullets, Random.Range(2, 4), Random.Range(2, 4));
        for (int i = 0; i < tempBullets.Count; i++) {
            bool tmp = tempBullets[i]; int r = Random.Range(i, tempBullets.Count);
            tempBullets[i] = tempBullets[r]; tempBullets[r] = tmp;
        }
        for (int i = 0; i < tempBullets.Count; i++) bullets.Set(i, tempBullets[i]);
        bulletCount = tempBullets.Count;
    }

    void AddBulletsToList(List<bool> list, int real, int blank) {
        for (int i = 0; i < real; i++) list.Add(true); for (int i = 0; i < blank; i++) list.Add(false);
    }

    public void RequestShoot(bool shootSelf) {
        if (IsMyTurn() && !isWaitingNextRound && (hpUI == null || !hpUI.isAnimating)) RPC_Shoot(shootSelf);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Shoot(bool shootSelf) {
        if (bulletCount <= 0 || isWaitingNextRound) return;
        bool isReal = bullets[0];
        for (int i = 0; i < bulletCount - 1; i++) bullets.Set(i, bullets[i + 1]);
        bulletCount--;

        int damage = doubleDamage ? 2 : 1; doubleDamage = false; 

        if (isReal) {
            if (activePlayerIndex == 0) { if (shootSelf) player1HP -= damage; else player2HP -= damage; }
            else { if (shootSelf) player2HP -= damage; else player1HP -= damage; }
            ChangeTurn(); 
        } else { if (!shootSelf) ChangeTurn(); }

        RPC_AnimateHealth(player1HP, player2HP); 
        RPC_SyncGunPosition(); 

        if (bulletCount <= 0 && player1HP > 0 && player2HP > 0) { currentRound++; StartCoroutine(NextRoundRoutine()); }
        CheckGameOver();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_Cua() { doubleDamage = true; }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_CongTay() { isCuffed = true; }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_NuocNgot() {
        if (bulletCount > 0) {
            for (int i = 0; i < bulletCount - 1; i++) bullets.Set(i, bullets[i + 1]);
            bulletCount--;
            if (bulletCount <= 0 && player1HP > 0 && player2HP > 0) { currentRound++; StartCoroutine(NextRoundRoutine()); }
        }
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_BinhMau() {
        if (activePlayerIndex == 0) player1HP = Mathf.Min(player1HP + 1, maxHP);
        else player2HP = Mathf.Min(player2HP + 1, maxHP);
        RPC_AnimateHealth(player1HP, player2HP);
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_LoThuoc() {
        int heal = (Random.Range(0, 2) == 0) ? 1 : -1;
        if (activePlayerIndex == 0) player1HP += heal; else player2HP += heal;
        RPC_AnimateHealth(player1HP, player2HP);
    }

    void ChangeTurn() { if (isCuffed) { isCuffed = false; return; } activePlayerIndex = (activePlayerIndex == 0) ? 1 : 0; }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] 
    void RPC_AnimateHealth(int p1, int p2) { StartCoroutine(HealthAnimationSequence(p1, p2)); }
    
    IEnumerator HealthAnimationSequence(int p1, int p2) {
        if (hpUI == null) yield break;
        yield return hpUI.StartCoroutine(hpUI.ShowHealthGroups());
        UpdateUIWithSpecificHP(p1, p2);
        yield return new WaitForSeconds(2.0f);
        yield return hpUI.StartCoroutine(hpUI.HideHealthGroups());
    }

    void UpdateUIWithSpecificHP(int p1, int p2) {
        if (hpUI == null) return;
        int myIndex = (Runner.IsServer) ? 0 : 1;
        if (myIndex == 0) hpUI.UpdateHealthUI(p1, p2); else hpUI.UpdateHealthUI(p2, p1);
    }

    void Update() {
        if (Object == null || Runner == null) return;
        if (shotCanvas != null) 
            shotCanvas.SetActive(IsMyTurn() && !isWaitingNextRound && (hpUI == null || !hpUI.isAnimating));
    }

    bool IsMyTurn() {
        if (Runner.LocalPlayer == PlayerRef.None) return false;
        int myIndex = (Runner.IsServer) ? 0 : 1;
        return myIndex == activePlayerIndex;
    }

    public bool GetCurrentBulletStatus() { return bullets[0]; }
    void CheckGameOver() { 
        if (player1HP <= 0) Debug.Log("PLAYER 2 THẮNG!"); if (player2HP <= 0) Debug.Log("PLAYER 1 THẮNG!"); 
    }
}