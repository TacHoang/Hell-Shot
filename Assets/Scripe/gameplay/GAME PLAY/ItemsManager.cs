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
    public ItemLogicHandler itemLogic;

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

    // --- HỆ THỐNG CONTROLLER (DÙNG ĐỂ GỌI ANIMATION) ---
    private static List<PlayerActionController> playerControllers = new List<PlayerActionController>();

    public static void RegisterPlayerController(PlayerActionController controller)
    {
        if (!playerControllers.Contains(controller)) playerControllers.Add(controller);
    }

    private void OnDestroy()
    {
        playerControllers.Clear();
    }

    private int[] lastLeftItems = new int[8];
    private int[] lastRightItems = new int[8];

    private int pendingSlotIndex = -1;
    private bool pendingFromLeft;

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

    // --- LOGIC HIỂN THỊ (RENDER) ---
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
                item.transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutBounce).SetLink(item);

                var clickScript = item.GetComponent<ItemClickDetector>() ?? item.AddComponent<ItemClickDetector>();
                clickScript.Setup(this, index, isLeft);
            }
        }
    }

    // --- LOGIC DÙNG VẬT PHẨM ---
    public void RequestUseItem(int slotIndex, bool fromLeft)
    {
        if (Object == null || !Object.IsValid) return;
        
        // Chặn click nếu đang diễn animation hoặc không phải lượt
        if (!gunManager.CanIInteract() || gunManager.isWaitingNextRound) return;

        int myIndex = Runner.IsServer ? 0 : 1;
        bool isMySide = (myIndex == 0 && fromLeft) || (myIndex == 1 && !fromLeft);
        if (!isMySide) return;

        RPC_ServerUseItem(fromLeft, slotIndex, Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_ServerUseItem(bool fromLeft, int slotIndex, PlayerRef user)
    {
        if (gunManager.isWaitingNextRound || gunManager.hasShotThisTurn || gunManager.isAnimatingAction) return;

        var targetArray = fromLeft ? leftItems : rightItems;
        int itemID = targetArray[slotIndex];

        if (itemID > 0)
        {
            // BƯỚC 1: KHÓA LOGIC
            gunManager.isAnimatingAction = true;

            pendingSlotIndex = slotIndex;
            pendingFromLeft = fromLeft;
            Debug.Log($"<color=yellow>[System]</color> Khởi chạy dùng Item ID: {itemID}. Canvas đã khóa.");

            // BƯỚC 2: TÌM CONTROLLER VÀ GỌI ANIMATION
            playerControllers.RemoveAll(c => c == null);
            var controller = playerControllers.FirstOrDefault(c => c.PlayerOwner == user);
            
            if (controller != null)
            {
                // Truyền ID vào controller trước để Event biết là món gì
                controller.SetCurrentItem(itemID);

                bool isRightSide = (slotIndex == 2 || slotIndex == 3 || slotIndex == 6 || slotIndex == 7);
                controller.RPC_PlayPickupAction(itemID, isRightSide);
            }
            else
            {
                Debug.LogError("[LỖI] Không tìm thấy Controller! Phá khóa khẩn cấp.");
                gunManager.isAnimatingAction = false; 
            }
        }
    }

    /// <summary>
    /// HÀM NÀY ĐƯỢC GỌI TỪ ANIMATION EVENT (OnPickupMoment)
    /// </summary>
    public void RealClearItem()
    {
        if (!HasStateAuthority) return;

        if (pendingSlotIndex != -1)
        {
            var targetArray = pendingFromLeft ? leftItems : rightItems;
            if (targetArray[pendingSlotIndex] != 0)
            {
                Debug.Log("<color=green>[Success]</color> Tay đã chạm đồ. Xóa dữ liệu Network Array.");
                targetArray.Set(pendingSlotIndex, 0); 
            }
            pendingSlotIndex = -1; 
        }

        // LƯU Ý: Không set isAnimatingAction = false ở đây nữa!
        // Hãy để Animation Event 'OnActionFinished' ở cuối clip lo việc đó.
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_PlayItemShrinkEffect(bool fromLeft, int slotIndex)
    {
        var slots = fromLeft ? leftSlots : rightSlots;
        var slot = slots[slotIndex];
        if (slot != null && slot.transform.childCount > 0)
        {
            var item = slot.transform.GetChild(0);
            if (item != null)
                item.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        }
    }

    GameObject GetPrefabByID(int id)
    {
        return id switch { 
            1 => glassPrefab, 2 => sawPrefab, 3 => cuffPrefab, 
            4 => sodaPrefab, 5 => pillPrefab, 6 => healthPrefab, _ => null 
        };
    }

    public void ShowTooltip(int index, bool isLeft) { }
    public void HideTooltip() { }
}