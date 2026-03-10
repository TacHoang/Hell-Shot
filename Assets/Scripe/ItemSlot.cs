using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    public ItemManager itemManager;
    public int itemID;

    public bool isPlayerItem;

    void OnMouseDown()
    {
        if(!isPlayerItem) return;

        Debug.Log("Click item");

        itemManager.UseItem(itemID);

        Destroy(gameObject);
    }
}