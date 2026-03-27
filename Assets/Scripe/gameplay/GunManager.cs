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

    // --- GAMEPLAY REFERENCES ---
    [Header("Core References")]
    public GameObject rotatingGun; 
    public Transform muzzlePoint; 
    public ItemsManager itemsManager; 
    public int maxHP = 5;

    [Header("Cinematic Settings")]
    public float zoomFOV = 30f;    
    public float normalFOV = 60f;  
    public float zoomDuration = 1.5f;

    [Header("UI References")]
    public HealthBarController hpUI; 
    public GameObject shotCanvas; 
    public GameObject roundPanel; 
    public TextMeshProUGUI roundText;
    private CanvasGroup roundCanvasGroup;

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
            canStartSequence = false; 

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
            yield return new WaitForSeconds(4.0f); 
            StartCoroutine(NextRoundRoutine());
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
            
            float startZ = rotatingGun.transform.localEulerAngles.z;
            float targetTotalZ = startZ + 3600f + Random.Range(0f, 360f); 

            float currentZ = startZ;
            DOTween.To(() => currentZ, x => currentZ = x, targetTotalZ, 10f)
                .SetEase(Ease.OutQuart)
                .OnUpdate(() => {
                    rotatingGun.transform.localRotation = Quaternion.Euler(90f, 0f, currentZ);
                });

            yield return new WaitForSeconds(10.5f);
            
            rotatingGun.transform.DOShakeRotation(0.5f, new Vector3(0, 0, 10), 10, 90);
            
            if (currentActiveCam != null)
                currentActiveCam.DOFieldOfView(normalFOV, 1f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(0.5f);

            if (HasStateAuthority)
            {
                float finalZ = rotatingGun.transform.localEulerAngles.z % 360f;
                if (finalZ < 0) finalZ += 360f;
                int winner = (finalZ > 180f) ? 0 : 1;
                RPC_FinalizeWinner(winner);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_FinalizeWinner(int winnerIdx)
    {
        activePlayerIndex = winnerIdx;
        RotateGunToActivePlayer();
    }

    void RotateGunToActivePlayer()
    {
        if (rotatingGun == null) return;
        rotatingGun.transform.DOKill();
        float targetZ = (activePlayerIndex == 0) ? -90f : 90f;
        rotatingGun.transform.DORotate(new Vector3(90f, 0f, targetZ), 0.8f).SetEase(Ease.OutBack);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_TriggerHealthIntro() { if (hpUI != null) hpUI.StartHealthIntro(); }

    public IEnumerator NextRoundRoutine() {
        if (!HasStateAuthority) yield break;

        isWaitingNextRound = true; 
        GenerateBullets();

        yield return new WaitForSeconds(1f);
        RPC_PlayRoundEffect(currentRound);
        
        yield return new WaitForSeconds(2.5f); 

        int itemsToGive = (currentRound == 1) ? 2 : Mathf.Min(currentRound, 4);
        if (itemsManager != null && itemsToGive > 0) 
        {
            itemsManager.GiveRandomItemsToBoth(itemsToGive);
            yield return new WaitForSeconds(1.5f); 
        }

        isWaitingNextRound = false; 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayRoundEffect(int roundNumber) {
        if (roundPanel == null || roundCanvasGroup == null) return;
        roundText.text = "ROUND " + roundNumber; 
        roundPanel.SetActive(true);
        roundCanvasGroup.alpha = 0f; 
        roundCanvasGroup.DOFade(1f, 0.5f).OnComplete(() => {
            roundCanvasGroup.DOFade(0f, 0.5f).SetDelay(1.5f).OnComplete(() => roundPanel.SetActive(false));
        });
    }

    void GenerateBullets() {
        if (!HasStateAuthority) return; 
        List<bool> tempBullets = new List<bool>();
        if (currentRound == 1) AddBulletsToList(tempBullets, 1, 1);
        else AddBulletsToList(tempBullets, Random.Range(2, 5), Random.Range(2, 5));
        
        for (int i = 0; i < tempBullets.Count; i++) {
            int r = Random.Range(i, tempBullets.Count);
            (tempBullets[i], tempBullets[r]) = (tempBullets[r], tempBullets[i]);
        }

        for (int i = 0; i < tempBullets.Count; i++) bullets.Set(i, tempBullets[i]);
        bulletCount = tempBullets.Count;
    }

    void AddBulletsToList(List<bool> list, int real, int blank) {
        for (int i = 0; i < real; i++) list.Add(true); 
        for (int i = 0; i < blank; i++) list.Add(false);
    }

    public void RequestShoot(bool shootSelf) {
        if (IsMyTurn() && !isWaitingNextRound && (hpUI == null || !hpUI.isAnimating)) 
            RPC_Shoot(shootSelf);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Shoot(bool shootSelf) {
        if (bulletCount <= 0 || isWaitingNextRound) return;

        bool isReal = bullets[0];
        for (int i = 0; i < bulletCount - 1; i++) bullets.Set(i, bullets[i + 1]);
        bulletCount--;

        int damage = doubleDamage ? 2 : 1; 
        doubleDamage = false; 

        bool shouldChangeTurn = true;
        if (isReal) {
            if (activePlayerIndex == 0) { if (shootSelf) player1HP -= damage; else player2HP -= damage; }
            else { if (shootSelf) player2HP -= damage; else player1HP -= damage; }
        } else {
            if (shootSelf) shouldChangeTurn = false;
        }

        player1HP = Mathf.Max(0, player1HP);
        player2HP = Mathf.Max(0, player2HP);

        RPC_AnimateHealth(player1HP, player2HP); 

        if (shouldChangeTurn) ChangeTurn();
        RPC_SyncVisuals(); 

        if (bulletCount <= 0 && player1HP > 0 && player2HP > 0) 
        { 
            currentRound++; 
            StartCoroutine(WaitForHealthThenRound()); 
        }
        CheckGameOver();
    }

    IEnumerator WaitForHealthThenRound()
    {
        yield return new WaitForSeconds(4.0f); 
        StartCoroutine(NextRoundRoutine());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SyncVisuals() => RotateGunToActivePlayer();

    // --- CÁC HÀM ITEM ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_Cua() { doubleDamage = true; }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_UseItem_CongTay() { isCuffed = true; }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
    public void RPC_UseItem_NuocNgot() {
        if (bulletCount > 0) {
            for (int i = 0; i < bulletCount - 1; i++) bullets.Set(i, bullets[i + 1]);
            bulletCount--;
            if (bulletCount <= 0 && player1HP > 0 && player2HP > 0) { currentRound++; StartCoroutine(NextRoundRoutine()); }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
    public void RPC_UseItem_BinhMau() {
        if (activePlayerIndex == 0) player1HP = Mathf.Min(player1HP + 1, maxHP);
        else player2HP = Mathf.Min(player2HP + 1, maxHP);
        RPC_AnimateHealth(player1HP, player2HP);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
    public void RPC_UseItem_LoThuoc() {
        int heal = (Random.Range(0, 2) == 0) ? 1 : -1;
        if (activePlayerIndex == 0) player1HP = Mathf.Clamp(player1HP + heal, 0, maxHP); 
        else player2HP = Mathf.Clamp(player2HP + heal, 0, maxHP);
        RPC_AnimateHealth(player1HP, player2HP);
        CheckGameOver();
    }

    void ChangeTurn() { 
        if (isCuffed) { isCuffed = false; return; } 
        activePlayerIndex = (activePlayerIndex == 0) ? 1 : 0; 
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] 
    void RPC_AnimateHealth(int p1, int p2) { 
        StopCoroutine("HealthAnimationSequence");
        StartCoroutine(HealthAnimationSequence(p1, p2)); 
    }
    
    IEnumerator HealthAnimationSequence(int p1, int p2) {
        if (hpUI == null) yield break;
        
        // 🔥 Cập nhật máu trước khi bắt đầu hiện thanh UI để tim mọc ra luôn
        UpdateUIWithSpecificHP(p1, p2);
        
        yield return hpUI.StartCoroutine(hpUI.ShowHealthGroups());
        yield return new WaitForSeconds(2.0f);
        yield return hpUI.StartCoroutine(hpUI.HideHealthGroups());
    }

    void UpdateUIWithSpecificHP(int p1, int p2) {
        if (hpUI == null) return;
        // Kiểm tra mình là Host hay Client để đảo bên UI cho đúng
        int myIndex = (Runner.IsServer) ? 0 : 1;
        if (myIndex == 0) hpUI.UpdateHealthUI(p1, p2); 
        else hpUI.UpdateHealthUI(p2, p1);
    }

    void Update() {
        if (Object == null || Runner == null) return;
        if (shotCanvas != null) 
        {
            bool showButtons = canStartSequence && IsMyTurn() && !isWaitingNextRound && (hpUI == null || !hpUI.isAnimating);
            shotCanvas.SetActive(showButtons);
        }
    }

    public bool IsMyTurn() 
    {
        if (Runner == null || Runner.LocalPlayer == PlayerRef.None) return false;
        
        // Host (Server) luôn là index 0, Client là index 1
        int myIndex = (Runner.IsServer) ? 0 : 1;
        
        // Trả về true nếu máy hiện tại khớp với người đang được quyền hành động
        return myIndex == activePlayerIndex;
    }

    public bool GetCurrentBulletStatus() { return bulletCount > 0 && bullets[0]; }
    
    void CheckGameOver() { 
        if (player1HP <= 0) Debug.Log("<color=red>GAME OVER: PLAYER 2 THẮNG!</color>"); 
        if (player2HP <= 0) Debug.Log("<color=red>GAME OVER: PLAYER 1 THẮNG!</color>"); 
    }
}