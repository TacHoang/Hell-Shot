using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TMPro;
using DG.Tweening;

public class GunManager : NetworkBehaviour
{
    [Header("Network Data")]
    [Networked, Capacity(8)] public NetworkArray<NetworkBool> bullets { get; }
    [Networked] public int bulletCount { get; set; }
    [Networked] public int player1HP { get; set; }
    [Networked] public int player2HP { get; set; }
    [Networked] public int activePlayerIndex { get; set; }
    [Networked] public int currentRound { get; set; }
    [Networked] public NetworkBool isWaitingNextRound { get; set; }

    [Header("Settings")]
    public int maxHP = 5;
    public GameObject shotCanvas; 
    
    [Header("UI Round Settings")]
    public GameObject roundPanel; 
    public TextMeshProUGUI roundText;
    private CanvasGroup roundCanvasGroup;

    [Header("Health UI Settings")]
    public HealthBarController hpUI; 

    public bool doubleDamage = false; 

    public override void Spawned()
    {
        if (roundPanel != null) 
            roundCanvasGroup = roundPanel.GetComponent<CanvasGroup>();

        if (HasStateAuthority)
        {
            player1HP = maxHP;
            player2HP = maxHP;
            currentRound = 1;
            activePlayerIndex = 0;
            StartCoroutine(NextRoundRoutine());
        }

        if (hpUI != null) hpUI.StartHealthIntro();
    }

    IEnumerator NextRoundRoutine()
    {
        if (!HasStateAuthority) yield break;
        isWaitingNextRound = true; 
        GenerateBullets();

        yield return new WaitForSeconds(2.0f);
        RPC_PlayRoundEffect(currentRound);

        yield return new WaitForSeconds(2.5f); 
        isWaitingNextRound = false; 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayRoundEffect(int roundNumber)
    {
        if (roundPanel == null || roundCanvasGroup == null) return;
        roundText.text = "ROUND " + roundNumber;
        roundPanel.SetActive(true);
        roundCanvasGroup.alpha = 0f;

        roundCanvasGroup.DOFade(1f, 0.5f).OnComplete(() => {
            roundCanvasGroup.DOFade(0f, 0.5f).SetDelay(1.5f).OnComplete(() => {
                roundPanel.SetActive(false);
            });
        });
    }

    void GenerateBullets()
    {
        if (!HasStateAuthority) return;
        List<bool> tempBullets = new List<bool>();
        int rand;

        if (currentRound == 1) { AddBulletsToList(tempBullets, 1, 1); }
        else if (currentRound == 2) { rand = Random.Range(0, 3); if (rand == 0) AddBulletsToList(tempBullets, 2, 2); else if (rand == 1) AddBulletsToList(tempBullets, 1, 3); else AddBulletsToList(tempBullets, 3, 1); }
        else if (currentRound == 3) { rand = Random.Range(0, 2); if (rand == 0) AddBulletsToList(tempBullets, 2, 3); else AddBulletsToList(tempBullets, 3, 2); }
        else if (currentRound == 4) { rand = Random.Range(0, 3); if (rand == 0) AddBulletsToList(tempBullets, 3, 3); else if (rand == 1) AddBulletsToList(tempBullets, 2, 4); else AddBulletsToList(tempBullets, 4, 2); }
        else if (currentRound == 5) { rand = Random.Range(0, 4); if (rand == 0) AddBulletsToList(tempBullets, 4, 3); else if (rand == 1) AddBulletsToList(tempBullets, 3, 4); else if (rand == 2) AddBulletsToList(tempBullets, 2, 5); else AddBulletsToList(tempBullets, 5, 2); }
        else { rand = Random.Range(0, 5); if (rand == 0) AddBulletsToList(tempBullets, 4, 4); else if (rand == 1) AddBulletsToList(tempBullets, 3, 5); else if (rand == 2) AddBulletsToList(tempBullets, 5, 3); else if (rand == 3) AddBulletsToList(tempBullets, 2, 6); else if (rand == 4) AddBulletsToList(tempBullets, 6, 2); }

        for (int i = 0; i < tempBullets.Count; i++)
        {
            bool tmp = tempBullets[i];
            int randomIndex = Random.Range(i, tempBullets.Count);
            tempBullets[i] = tempBullets[randomIndex];
            tempBullets[randomIndex] = tmp;
        }

        for (int i = 0; i < tempBullets.Count; i++) bullets.Set(i, tempBullets[i]);
        bulletCount = tempBullets.Count;
    }

    void AddBulletsToList(List<bool> list, int real, int blank)
    {
        for (int i = 0; i < real; i++) list.Add(true);
        for (int i = 0; i < blank; i++) list.Add(false);
    }

    public void RequestShoot(bool shootSelf)
    {
        if (IsMyTurn() && !isWaitingNextRound && (hpUI == null || !hpUI.isAnimating))
        {
            RPC_Shoot(shootSelf);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Shoot(bool shootSelf)
    {
        if (bulletCount <= 0 || isWaitingNextRound) return;

        bool isReal = bullets[0];
        for (int i = 0; i < bulletCount - 1; i++) bullets.Set(i, bullets[i + 1]);
        bulletCount--;

        int damage = doubleDamage ? 2 : 1;
        doubleDamage = false;
        bool shouldChangeTurn = true; 

        if (isReal)
        {
            if (shootSelf) { if (activePlayerIndex == 0) player1HP -= damage; else player2HP -= damage; }
            else { if (activePlayerIndex == 0) player2HP -= damage; else player1HP -= damage; }
            shouldChangeTurn = true; 
        }
        else 
        {
            if (shootSelf) shouldChangeTurn = false; else shouldChangeTurn = true;
        }

        // Truyền máu mới trực tiếp vào RPC để client chạy ngay
        RPC_AnimateHealth(player1HP, player2HP); 

        if (shouldChangeTurn) ChangeTurn();
        CheckGameOver();

        if (bulletCount <= 0)
        {
            currentRound++;
            StartCoroutine(NextRoundRoutine());
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_AnimateHealth(int p1HP, int p2HP)
    {
        StartCoroutine(HealthAnimationSequence(p1HP, p2HP));
    }

    IEnumerator HealthAnimationSequence(int p1HP, int p2HP)
    {
        if (hpUI == null) yield break;

        // 1. Chạy thanh máu ra
        yield return hpUI.StartCoroutine(hpUI.ShowHealthGroups());

        // 2. Nghỉ một chút
        yield return new WaitForSeconds(0.5f);

        // 3. Thực hiện rơi tim dựa trên số máu vừa nhận từ RPC
        UpdateUIWithSpecificHP(p1HP, p2HP);

        // 4. Đợi hiệu ứng rơi tim và nghỉ
        yield return new WaitForSeconds(2.0f);

        // 5. Chạy thanh máu vào
        yield return hpUI.StartCoroutine(hpUI.HideHealthGroups());
    }

    // Hàm phụ để cập nhật máu chính xác từ dữ liệu RPC
    void UpdateUIWithSpecificHP(int p1, int p2)
    {
        if (hpUI == null) return;
        var allPlayers = Runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        int myLocalIndex = allPlayers.IndexOf(Runner.LocalPlayer);

        if (myLocalIndex == 0)
            hpUI.UpdateHealthUI(p1, p2);
        else
            hpUI.UpdateHealthUI(p2, p1);
    }

    void ChangeTurn() { activePlayerIndex = (activePlayerIndex == 0) ? 1 : 0; }

    void CheckGameOver() { if (player1HP <= 0) Debug.Log("PLAYER 2 THẮNG!"); if (player2HP <= 0) Debug.Log("PLAYER 1 THẮNG!"); }

    void Update()
    {
        if (Object == null || Runner == null) return;

        if (hpUI != null && !hpUI.isAnimating)
        {
            RefreshHealthUI();
        }

        if (shotCanvas != null)
        {
            shotCanvas.SetActive(IsMyTurn() && !isWaitingNextRound && (hpUI == null || !hpUI.isAnimating));
        }
    }

    void RefreshHealthUI()
    {
        if (hpUI == null) return;
        var allPlayers = Runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        int myLocalIndex = allPlayers.IndexOf(Runner.LocalPlayer);

        if (myLocalIndex == 0)
            hpUI.UpdateHealthUI(player1HP, player2HP);
        else
            hpUI.UpdateHealthUI(player2HP, player1HP);
    }

    bool IsMyTurn()
    {
        if (Runner.LocalPlayer == PlayerRef.None) return false;
        var allPlayers = Runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        int myIndex = allPlayers.IndexOf(Runner.LocalPlayer);
        return myIndex == activePlayerIndex;
    }
}