using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Thêm cái này để xử lý danh sách Player
using DG.Tweening;

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

    [Networked, Capacity(8)] public NetworkArray<int> leftItems { get; }
    [Networked, Capacity(8)] public NetworkArray<int> rightItems { get; }

    private int[] lastLeftItems = new int[8];
    private int[] lastRightItems = new int[8];

    // --- LOGIC CẤP ĐỒ (Chỉ Server chạy) ---
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

    // --- LOGIC HIỂN THỊ (Đồng bộ hóa Visual) ---
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
        
        // Xóa vật thể cũ trong slot
        foreach (Transform child in slot.transform) Destroy(child.gameObject);

        if (id > 0)
        {
            GameObject prefab = GetPrefabByID(id);
            if (prefab != null)
            {
                GameObject item = Instantiate(prefab, slot.transform);
                item.transform.localPosition = new Vector3(0, 2f, 0); // Sinh ra từ trên cao
                item.transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutBounce);

                // Gán detector click cho vật thể 3D vừa tạo
                var clickScript = item.GetComponent<ItemClickDetector>() ?? item.AddComponent<ItemClickDetector>();
                clickScript.Setup(this, index, isLeft);
            }
        }
    }

    // --- LOGIC DÙNG VẬT PHẨM ---
    public void RequestUseItem(int slotIndex, bool fromLeft)
    {
        if (Object == null || !Object.IsValid) return;

        // 1. Kiểm tra lượt (Chặn ngay tại Client)
        if (!gunManager.IsMyTurn()) return;

        // 2. Kiểm tra bấm đúng phía của mình không
        int myIndex = Runner.IsServer ? 0 : 1;
        bool isMySide = (myIndex == 0 && fromLeft) || (myIndex == 1 && !fromLeft);
        if (!isMySide) return;

        RPC_ServerUseItem(fromLeft, slotIndex, Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_ServerUseItem(bool fromLeft, int slotIndex, PlayerRef user)
    {
        // --- ĐOẠN ĐÃ SỬA: Xác minh người gửi không bị gạch đỏ ---
        // Lấy danh sách người chơi đang active và tìm index của người gửi RPC
        var players = Runner.ActivePlayers.ToList();
        int senderIndex = players.IndexOf(user);

        // Nếu index không khớp với người đang có lượt thì hủy lệnh
        if (senderIndex != gunManager.activePlayerIndex) 
        {
            Debug.LogWarning($"Player {senderIndex} cố dùng đồ sai lượt!");
            return; 
        }

        var targetArray = fromLeft ? leftItems : rightItems;
        int itemID = targetArray[slotIndex];
        if (itemID <= 0) return;

        switch (itemID)
        {
            case 1: // KÍNH LÚP
                RPC_ShowGlassResult(user, gunManager.GetCurrentBulletStatus());
                break; 
            case 2: gunManager.RPC_UseItem_Cua(); break;
            case 3: gunManager.RPC_UseItem_CongTay(); break;
            case 4: gunManager.RPC_UseItem_NuocNgot(); break;
            case 5: gunManager.RPC_UseItem_LoThuoc(); break;
            case 6: gunManager.RPC_UseItem_BinhMau(); break;
        }

        targetArray.Set(slotIndex, 0); // Xóa khỏi NetworkArray để Render tự xóa Visual
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    void RPC_ShowGlassResult([RpcTarget] PlayerRef target, bool isReal)
    {
        // Hiển thị kết quả soi đạn chỉ cho người dùng
        Debug.Log($"<color={(isReal ? "red" : "white")}>[SOI ĐẠN] Kết quả: {(isReal ? "ĐẠN THẬT" : "ĐẠN GIẢ")}</color>");
    }

    GameObject GetPrefabByID(int id)
    {
        return id switch { 1 => glassPrefab, 2 => sawPrefab, 3 => cuffPrefab, 4 => sodaPrefab, 5 => pillPrefab, 6 => healthPrefab, _ => null };
    }
}