using Fusion;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class ItemsManager : NetworkBehaviour
{
    [Header("References")]
    public GunManager gunManager;

    [Header("Slots Configuration")]
    public GameObject[] leftSlots;  // 8 ô bên TRÁI bàn (Dành cho P1)
    public GameObject[] rightSlots; // 8 ô bên PHẢI bàn (Dành cho P2)

    [Header("Item Prefabs")]
    public GameObject glassPrefab;  // ID 1
    public GameObject sawPrefab;    // ID 2
    public GameObject cuffPrefab;   // ID 3
    public GameObject sodaPrefab;   // ID 4
    public GameObject pillPrefab;   // ID 5
    public GameObject healthPrefab; // ID 6

    // Đồng bộ ID vật phẩm trong 8 ô trái và 8 ô phải
    [Networked, Capacity(8)] public NetworkArray<int> leftItems { get; }
    [Networked, Capacity(8)] public NetworkArray<int> rightItems { get; }

    public override void Spawned()
    {
        // ĐÃ XÓA: GiveRandomItemsToBoth(2) để tránh việc vừa chuyển scene đã có đồ.
    }

    // Hàm này sẽ được GunManager gọi mỗi khi bắt đầu Round mới
    public void GiveRandomItemsToBoth(int amount)
    {
        if (!HasStateAuthority) return;

        for (int i = 0; i < amount; i++)
        {
            AddItemToSide(true, Random.Range(1, 7));  // Thêm vào bên Trái
            AddItemToSide(false, Random.Range(1, 7)); // Thêm vào bên Phải
        }
    }

    void AddItemToSide(bool isLeft, int itemID)
    {
        var targetArray = isLeft ? leftItems : rightItems;
        for (int i = 0; i < 8; i++)
        {
            if (targetArray[i] == 0) 
            {
                targetArray.Set(i, itemID);
                break;
            }
        }
    }

    public override void Render()
    {
        // Tự động hiển thị item lên các slot tương ứng trên bàn
        UpdateSlotVisuals(leftItems, leftSlots);
        UpdateSlotVisuals(rightItems, rightSlots);
    }

    void UpdateSlotVisuals(NetworkArray<int> items, GameObject[] slots)
    {
        for (int i = 0; i < 8; i++)
        {
            if (slots[i] == null) continue;

            int currentID = items[i];
            int childCount = slots[i].transform.childCount;

            if (currentID == 0)
            {
                if (childCount > 0)
                {
                    foreach (Transform child in slots[i].transform) Destroy(child.gameObject);
                }
            }
            else 
            {
                if (childCount == 0)
                {
                    GameObject prefab = GetPrefabByID(currentID);
                    if (prefab != null) 
                    {
                        GameObject item = Instantiate(prefab, slots[i].transform);
                        // HIỆU ỨNG RƠI: Đặt vị trí cao hơn 1 xíu rồi cho rớt xuống
                        Vector3 finalPos = item.transform.localPosition;
                        item.transform.localPosition = finalPos + new Vector3(0, 5f, 0); // Cao hơn 5 đơn vị
                        item.transform.DOLocalMove(finalPos, 0.5f).SetEase(Ease.OutBounce); // Rớt xuống nảy nhẹ
                    }
                }
            }
        }
    }

    GameObject GetPrefabByID(int id)
    {
        return id switch {
            1 => glassPrefab, 2 => sawPrefab, 3 => cuffPrefab,
            4 => sodaPrefab, 5 => pillPrefab, 6 => healthPrefab,
            _ => null
        };
    }

    // Hàm gọi khi người chơi Click vào vật phẩm trên bàn
    public void RequestUseItem(int slotIndex)
    {
        bool amILeft = (Runner.IsServer); 
        RPC_ServerUseItem(amILeft, slotIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_ServerUseItem(bool fromLeft, int slotIndex)
    {
        var targetArray = fromLeft ? leftItems : rightItems;
        int itemID = targetArray[slotIndex];

        if (itemID == 0) return;

        // Kích hoạt công dụng trong GunManager
        switch (itemID)
        {
            case 2: gunManager.RPC_UseItem_Cua(); break;
            case 3: gunManager.RPC_UseItem_CongTay(); break;
            case 4: gunManager.RPC_UseItem_NuocNgot(); break;
            case 5: gunManager.RPC_UseItem_LoThuoc(); break;
            case 6: gunManager.RPC_UseItem_BinhMau(); break;
        }

        // Dùng xong thì xóa ID trong mảng (Render sẽ tự xóa hình trên bàn)
        targetArray.Set(slotIndex, 0);
    }
}