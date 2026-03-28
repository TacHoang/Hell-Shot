using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro; 

public class ItemsManager : NetworkBehaviour
{
    [Header("References")]
    public GunManager gunManager;

    [Header("Slots Configuration")]
    public GameObject[] leftSlots;  // 8 ô bên P1
    public GameObject[] rightSlots; // 8 ô bên P2

    [Header("Item Prefabs")]
    public GameObject glassPrefab;  // ID 1
    public GameObject sawPrefab;    // ID 2
    public GameObject cuffPrefab;   // ID 3
    public GameObject sodaPrefab;   // ID 4
    public GameObject pillPrefab;   // ID 5
    public GameObject healthPrefab; // ID 6

    [Header("UI Tooltip")]
    public GameObject tooltipPanel;    
    public TextMeshProUGUI tooltipText; 

    [Networked, Capacity(8)] public NetworkArray<int> leftItems { get; }
    [Networked, Capacity(8)] public NetworkArray<int> rightItems { get; }

    private int[] lastLeftItems = new int[8];
    private int[] lastRightItems = new int[8];

    // --- LOGIC CẤP ĐỒ ---
    public void GiveRandomItemsToBoth(int amount)
    {
        if (!HasStateAuthority) return;
        for (int i = 0; i < amount; i++)
        {
            AddItemToSide(true, Random.Range(1, 7));  
            AddItemToSide(false, Random.Range(1, 7)); 
        }
    }

    void AddItemToSide(bool isLeft, int itemID)
    {
        var targetArray = isLeft ? leftItems : rightItems;
        for (int i = 0; i < 8; i++)
        {
            if (targetArray[i] == 0) {
                targetArray.Set(i, itemID);
                break;
            }
        }
    }

    // --- LOGIC HIỂN THỊ ---
    public override void Render()
    {
        SyncVisuals(leftItems, leftSlots, lastLeftItems, true);
        SyncVisuals(rightItems, rightSlots, lastRightItems, false);
    }

    void SyncVisuals(NetworkArray<int> networkItems, GameObject[] slots, int[] cache, bool isLeft)
    {
        for (int i = 0; i < 8; i++)
        {
            if (networkItems[i] != cache[i])
            {
                UpdateSingleSlot(slots[i], networkItems[i], i, isLeft);
                cache[i] = networkItems[i];
            }
        }
    }

    void UpdateSingleSlot(GameObject slot, int id, int index, bool isLeft)
    {
        if (slot == null) return;
        foreach (Transform child in slot.transform) Destroy(child.gameObject);

        if (id > 0)
        {
            GameObject prefab = GetPrefabByID(id);
            if (prefab != null)
            {
                GameObject item = Instantiate(prefab, slot.transform);
                item.transform.localPosition = new Vector3(0, 2f, 0);
                item.transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutBounce);

                var clickScript = item.GetComponent<ItemClickDetector>() ?? item.AddComponent<ItemClickDetector>();
                clickScript.Setup(this, index, isLeft);
            }
        }
    }

    // --- HÀM TOOLTIP ---
    public void ShowTooltip(int index, bool isLeft)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        int itemID = isLeft ? leftItems[index] : rightItems[index];
        if (itemID <= 0) return;

        tooltipPanel.SetActive(true);
        tooltipText.text = GetItemDescription(itemID);
        
        tooltipPanel.transform.DOKill();
        tooltipPanel.transform.localScale = Vector3.zero;
        tooltipPanel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
    }

    public void HideTooltip()
    {
        if (tooltipPanel == null) return;
        tooltipPanel.transform.DOKill();
        tooltipPanel.transform.DOScale(0f, 0.15f).OnComplete(() => tooltipPanel.SetActive(false));
    }

    string GetItemDescription(int id)
    {
        return id switch {
            1 => "KÍNH LÚP: Xem viên đạn trong nòng là thật hay giả.",
            2 => "LƯỠI CƯA: Tăng gấp đôi sát thương cho phát bắn tiếp theo.",
            3 => "CÒNG TAY: Khóa lượt chơi của đối phương ở vòng sau.",
            4 => "LON SODA: Loại bỏ viên đạn hiện tại ra khỏi súng.",
            5 => "LÔ THUỐC: 50% hồi 1 máu, 50% bị trừ 1 máu.",
            6 => "BÌNH MÁU: Hồi phục ngay lập tức 1 máu.",
            _ => "Vật phẩm lạ."
        };
    }

    // --- LOGIC DÙNG VẬT PHẨM ---
    public void RequestUseItem(int slotIndex, bool fromLeft)
    {
        if (Object == null || !Object.IsValid) return;

        // CHỈNH SỬA TẠI ĐÂY: Chặn dùng đồ nếu không phải lượt hoặc đang chờ chuyển Round
        if (!gunManager.IsMyTurn() || gunManager.isWaitingNextRound) return;

        int myIndex = Runner.IsServer ? 0 : 1;
        bool isMySide = (myIndex == 0 && fromLeft) || (myIndex == 1 && !fromLeft);
        if (!isMySide) return;

        RPC_ServerUseItem(fromLeft, slotIndex, Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_ServerUseItem(bool fromLeft, int slotIndex, PlayerRef user)
    {
        // Chặn thêm một lần nữa trên Server để đảm bảo an toàn tuyệt đối
        if (gunManager.isWaitingNextRound) return;

        var players = Runner.ActivePlayers.ToList();
        int senderIndex = players.IndexOf(user);

        if (senderIndex != gunManager.activePlayerIndex) return;

        var targetArray = fromLeft ? leftItems : rightItems;
        int itemID = targetArray[slotIndex];
        if (itemID <= 0) return;

        switch (itemID)
        {
            case 1: RPC_ShowGlassResult(user, gunManager.GetCurrentBulletStatus()); break; 
            case 2: gunManager.RPC_UseItem_Cua(); break;
            case 3: gunManager.RPC_UseItem_CongTay(); break;
            case 4: gunManager.RPC_UseItem_NuocNgot(); break;
            case 5: gunManager.RPC_UseItem_LoThuoc(); break;
            case 6: gunManager.RPC_UseItem_BinhMau(); break;
        }

        targetArray.Set(slotIndex, 0);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_ShowGlassResult([RpcTarget] PlayerRef target, bool isReal)
    {
        Debug.Log($"<color={(isReal ? "red" : "white")}>[SOI ĐẠN] Kết quả: {(isReal ? "ĐẠN THẬT" : "ĐẠN GIẢ")}</color>");
    }

    GameObject GetPrefabByID(int id)
    {
        return id switch { 1 => glassPrefab, 2 => sawPrefab, 3 => cuffPrefab, 4 => sodaPrefab, 5 => pillPrefab, 6 => healthPrefab, _ => null };
    }
}