using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;

public class ItemsManager : NetworkBehaviour
{
    // --- SINGLETON PATTERN ---
    public static ItemsManager Instance { get; private set; }

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

    // BIẾN MẠNG ĐỂ ĐỒNG BỘ VIỆC NHẶT ĐỒ
    [Networked] public int networkedPendingSlot { get; set; } = -1;
    [Networked] public NetworkBool networkedPendingFromLeft { get; set; }

    private static List<PlayerActionController> playerControllers = new List<PlayerActionController>();

    public static void RegisterPlayerController(PlayerActionController controller)
    {
        if (!playerControllers.Contains(controller)) playerControllers.Add(controller);
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        playerControllers.Clear();
        if (Instance == this) Instance = null;
    }

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
            gunManager.isAnimatingAction = true;

            // LƯU BIẾN MẠNG: Để tí nữa Animation Event biết slot nào mà ẩn
            networkedPendingSlot = slotIndex;
            networkedPendingFromLeft = fromLeft;

            playerControllers.RemoveAll(c => c == null);
            var controller = playerControllers.FirstOrDefault(c => c.PlayerOwner == user);
            
            if (controller != null)
            {
                controller.SetCurrentItem(itemID);
                bool isRightSide = (slotIndex == 2 || slotIndex == 3 || slotIndex == 6 || slotIndex == 7);
                controller.RPC_PlayPickupAction(itemID, isRightSide);
            }
            else
            {
                gunManager.isAnimatingAction = false; 
            }
        }
    }

    /// <summary>
    /// Hiệu ứng làm biến mất vật phẩm trên bàn ngay lập tức (Gọi từ PlayerActionController)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_HideWorldItemVisual(bool fromLeft, int slotIndex)
    {
        var slots = fromLeft ? leftSlots : rightSlots;
        if (slotIndex >= 0 && slotIndex < slots.Length)
        {
            GameObject slot = slots[slotIndex];
            if (slot.transform.childCount > 0)
            {
                Transform item = slot.transform.GetChild(0);
                // Hiệu ứng co nhỏ rồi ẩn đi
                item.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
                    if(item != null) item.gameObject.SetActive(false);
                });
            }
        }
    }

    /// <summary>
    /// Xóa dữ liệu vật phẩm trong NetworkArray (Gọi ở cuối chuỗi animation)
    /// </summary>
    public void RealClearItem()
    {
        if (!HasStateAuthority) return;

        if (networkedPendingSlot != -1)
        {
            var targetArray = networkedPendingFromLeft ? leftItems : rightItems;
            if (targetArray[networkedPendingSlot] != 0)
            {
                targetArray.Set(networkedPendingSlot, 0); 
            }
            networkedPendingSlot = -1; // Reset slot chờ
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