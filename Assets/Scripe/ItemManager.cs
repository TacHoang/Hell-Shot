using UnityEngine;
using System.Collections;

public class ItemManager : MonoBehaviour
{
    public GunManager gun;

    public GameObject[] itemPrefabs;

    public Transform[] playerSlots;
    public Transform[] enemySlots;

    public void UseItem(int id)
    {
        switch (id)
        {
            case 0: UseMagnifyingGlass(); break;
            case 1: UseSaw(); break;
            case 2: UseBeer(); break;
            case 3: UseCigarette(); break;
            case 4: UseHandcuffs(); break;
            case 5: UseBurnerPhone(); break;
            case 6: UseInverter(); break;
            case 7: UseExpiredMedicine(); break;
            case 8: UseAdrenaline(); break;
        }
    }

    int GetRandomItem()
    {
        if (itemPrefabs.Length == 0)
        {
            Debug.LogError("Chưa gán Item Prefabs!");
            return 0;
        }

        return Random.Range(0, itemPrefabs.Length);
    }

    int GetEmptySlots(Transform[] slots)
    {
        int count = 0;

        foreach (Transform slot in slots)
        {
            if (slot.childCount == 0)
                count++;
        }

        return count;
    }

    // ⭐ SỬA HÀM NÀY
    void GiveItem(Transform[] slots, int amount, bool isPlayer)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (amount <= 0) break;

            Transform slot = slots[i];

            if (slot.childCount == 0)
            {
                int random = Random.Range(0, itemPrefabs.Length);

                GameObject item = Instantiate(itemPrefabs[random]);

                item.transform.SetParent(slot);
                item.transform.localPosition = Vector3.zero;

                ItemSlot itemSlot = item.GetComponent<ItemSlot>();

                if (itemSlot != null)
                {
                    itemSlot.itemManager = this;
                    itemSlot.isPlayerItem = isPlayer;
                }

                amount--;
            }
        }
    }

    public void GiveItemsByRound(int round)
    {
        int itemAmount = 0;

        if (round == 1) itemAmount = 0;
        else if (round == 2) itemAmount = 2;
        else if (round == 3) itemAmount = 3;
        else if (round == 4) itemAmount = 4;
        else itemAmount = 5;

        int playerEmpty = GetEmptySlots(playerSlots);
        int enemyEmpty = GetEmptySlots(enemySlots);

        int playerGive = Mathf.Min(itemAmount, playerEmpty);
        int enemyGive = Mathf.Min(itemAmount, enemyEmpty);

        // ⭐ PHÂN BIỆT PLAYER / ENEMY
        GiveItem(playerSlots, playerGive, true);
        GiveItem(enemySlots, enemyGive, false);
    }

    public void UseMagnifyingGlass()
    {
        if (gun.bullets.Count == 0) return;

        bool bullet = gun.bullets[0];
        Debug.Log(bullet ? "Đạn thật" : "Đạn rỗng");
    }

    public void UseSaw()
    {
        gun.doubleDamage = true;
        Debug.Log("Damage x2");
    }

    public void UseBeer()
    {
        if (gun.bullets.Count == 0) return;

        gun.bullets.RemoveAt(0);
        Debug.Log("Đã bỏ viên đạn");
    }

    public void UseCigarette()
    {
        gun.playerHP += 1;
        Debug.Log("Hồi 1 máu");
    }

    public void UseHandcuffs()
    {
        gun.enemySkipTurn = true;
        Debug.Log("Enemy mất lượt");
    }

    public void UseBurnerPhone()
    {
        if (gun.bullets.Count == 0) return;

        int index = Random.Range(0, gun.bullets.Count);

        Debug.Log(gun.bullets[index] ? "Random bullet: ĐẠN THẬT" : "Random bullet: ĐẠN RỖNG");
    }

    public void UseInverter()
    {
        if (gun.bullets.Count == 0) return;

        gun.bullets[0] = !gun.bullets[0];

        Debug.Log("Đã đảo đạn");
    }

    public void UseExpiredMedicine()
    {
        float rand = Random.value;

        if (rand < 0.4f)
        {
            gun.playerHP += 2;
            Debug.Log("Hồi 2 máu");
        }
        else
        {
            gun.playerHP -= 1;
            Debug.Log("Mất 1 máu");
        }
    }

    public void UseAdrenaline()
    {
        Debug.Log("Cướp item (chưa làm inventory bot)");
    }

    public IEnumerator BotSmartUse()
    {
        ItemSlot magnifier = null;
        ItemSlot saw = null;
        ItemSlot medicine = null;
        ItemSlot inverter = null;

        foreach (Transform slot in enemySlots)
        {
            if (slot.childCount == 0) continue;

            ItemSlot item = slot.GetChild(0).GetComponent<ItemSlot>();

            if (item == null) continue;

            if (item.itemID == 0) magnifier = item;
            if (item.itemID == 1) saw = item;
            if (item.itemID == 7) medicine = item;
            if (item.itemID == 6) inverter = item;
        }

        // máu thấp → dùng thuốc
        if (gun.enemyHP <= 2 && medicine != null)
        {
            Debug.Log("Bot dùng thuốc");

            UseItem(medicine.itemID);
            Destroy(medicine.gameObject);

            yield return new WaitForSeconds(1f);
        }

        // dùng kính lúp
        if (magnifier != null)
        {
            Debug.Log("Bot dùng kính lúp");

            UseItem(magnifier.itemID);
            Destroy(magnifier.gameObject);

            yield return new WaitForSeconds(1f);
        }

        // nếu đạn rỗng → đảo đạn
        if (gun.bullets.Count > 0 && gun.bullets[0] == false && inverter != null)
        {
            Debug.Log("Bot dùng inverter");

            UseItem(inverter.itemID);
            Destroy(inverter.gameObject);

            yield return new WaitForSeconds(1f);
        }

        // nếu đạn thật → dùng cưa
        if (gun.bullets.Count > 0 && gun.bullets[0] == true && saw != null)
        {
            Debug.Log("Bot dùng cưa");

            UseItem(saw.itemID);
            Destroy(saw.gameObject);

            yield return new WaitForSeconds(1f);
        }
    }
}