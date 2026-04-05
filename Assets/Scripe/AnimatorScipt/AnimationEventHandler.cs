using Fusion;
using UnityEngine;

public class AnimationEventHandler : NetworkBehaviour
{
    public GameObject glassesPrefab;
    public GameObject gunPrefab;
    public Transform handTransform; // gắn bone tay nhân vật trong prefab
    private GameObject spawnedGlasses;
    private GameObject spawnedGun;

    // Event 1: kính xuất hiện trên tay
    public void OnShowGlassInHand()
    {
        spawnedGlasses = Instantiate(glassesPrefab, handTransform.position, handTransform.rotation, handTransform);
        spawnedGlasses.transform.SetParent(handTransform);
    }

    // Event 2: kính rơi ra, bay thẳng, destroy sau 5s
    public void OnThrowGlasses()
    {
        if (spawnedGlasses != null)
        {
            spawnedGlasses.transform.SetParent(null);
            Rigidbody rb = spawnedGlasses.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(transform.forward * 5f, ForceMode.Impulse);
            Destroy(spawnedGlasses, 5f);
        }
    }

    // Event 3: spawn súng và soi đạn
    public void OnTransformToGun()
    {
        spawnedGun = Instantiate(gunPrefab, handTransform.position, handTransform.rotation, handTransform);
        spawnedGun.transform.SetParent(handTransform);

        var itemsManager = FindObjectOfType<ItemsManager>();
        if (itemsManager != null)
            itemsManager.CheckBulletAuthenticity();
    }


    // Quay lại Sit khi nhấn chuột trái
    public void OnReturnToSit()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.Play("Sit");

        // Destroy item slot sau khi người chơi xác nhận
        var itemsManager = FindObjectOfType<ItemsManager>();
        if (itemsManager != null)
        {
            // Ví dụ: clear slot kính lúp đã dùng
            // Bạn có thể truyền index/side qua biến tạm để biết slot nào cần xóa
        }
    }

}
