using Fusion;
using UnityEngine;
using System.Collections;

public class ItemLogicHandler : NetworkBehaviour
{
    public GunManager gunManager;

    public void ExecuteItemLogic(int itemID, PlayerRef user, bool fromLeft)
    {
        if (!HasStateAuthority) return;

     //   gunManager.hasShotThisTurn = true;

        switch (itemID)
        {
            case 1: UseGlass(user); break;
            case 2: UseSaw(); break;
            case 3: UseCuff(user); break;
            case 4: UseSoda(); break;
            case 5: UsePill(user); return; // Thoát ra, để AnimateHealth lo nhả lock
            case 6: UseHealth(user); return; // Thoát ra, để AnimateHealth lo nhả lock
        }
    }


// Tìm hàm UseGlass và sửa lại tên hàm gọi
    private void UseGlass(PlayerRef user) 
    { 
        gunManager.RPC_ShowGlassResult(user, gunManager.GetCurrentBulletType()); 
    }
    private void UseSaw() { gunManager.doubleDamage = true; }
    private void UseCuff(PlayerRef user)
    {
        gunManager.isCuffed = true;
        gunManager.cuffedPlayerIndex = (gunManager.activePlayerIndex == 0) ? 1 : 0;
    }

    // --- SỬA LẠI HÀM UseSoda ---
private void UseSoda() {
    if (!HasStateAuthority) return; // Chỉ Host chạy logic

    if (gunManager.bulletCount <= 0) return;

    bool isReal = gunManager.bullets[0]; // Host lấy dữ liệu thật

    // Host bắn lệnh cho cả 2 máy cùng hiện đạn văng ra
    gunManager.RPC_AnimateSodaEject(isReal);

    // Host trừ đạn trong súng
    gunManager.EjectBullet(); 

    if (gunManager.bulletCount == 0) {
        // Chuyển round sau khi văng xong
        StartCoroutine(WaitThenNextRound());
    }
}

private IEnumerator WaitThenNextRound() {
    yield return new WaitForSeconds(1.5f);
    if (gunManager.player1HP > 0 && gunManager.player2HP > 0) {
        gunManager.currentRound++;
        gunManager.StartCoroutine(gunManager.NextRoundRoutine());
    }
}

    private void UsePill(PlayerRef user)
    {
        int chance = Random.Range(0, 2); 
        ModifyHealth(user, (chance == 0) ? 1 : -1);
    }

    private void UseHealth(PlayerRef user) { ModifyHealth(user, 1); }

    // --- SỬA LẠI HÀM ModifyHealth ---
    private void ModifyHealth(PlayerRef user, int amount)
    {
        // Đảm bảo chỉ Server thực hiện thay đổi dữ liệu Networked
        if (!HasStateAuthority) return;

        if (gunManager.activePlayerIndex == 0) 
            gunManager.player1HP = Mathf.Clamp(gunManager.player1HP + amount, 0, gunManager.maxHP);
        else 
            gunManager.player2HP = Mathf.Clamp(gunManager.player2HP + amount, 0, gunManager.maxHP);

        // false cuối cùng là để báo đây là hồi máu/pill, không phải bắn viên đạn cuối
        gunManager.RPC_AnimateHealth(gunManager.player1HP, gunManager.player2HP, false, false);
    }
}