using Fusion;
using UnityEngine;
using System.Collections;

public class ItemLogicHandler : NetworkBehaviour
{
    public GunManager gunManager;

    public void ExecuteItemLogic(int itemID, PlayerRef user, bool fromLeft)
    {
        if (!HasStateAuthority) return;

        // Khóa mạng ngay lập tức để không bấm thêm được gì
        gunManager.hasShotThisTurn = true;

        switch (itemID)
        {
            case 1: UseGlass(user); break;
            case 2: UseSaw(); break;
            case 3: UseCuff(); break;
            case 4: UseSoda(); break;
            case 5: UsePill(user); break; 
            case 6: UseHealth(user); break;
        }

        // Với các món không có Animation máu (1, 2, 3, 4), ta mở khóa sau 1 khoảng delay ngắn
        if (itemID <= 4) {
            StartCoroutine(UnlockAfterDelay(1.2f));
        }
    }

    IEnumerator UnlockAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Chỉ reset nếu không phải đang chờ đổi round
        if (!gunManager.isWaitingNextRound) {
            gunManager.hasShotThisTurn = false;
        }
    }

// Tìm hàm UseGlass và sửa lại tên hàm gọi
    private void UseGlass(PlayerRef user) 
    { 
        gunManager.RPC_ShowGlassResult(user, gunManager.GetCurrentBulletType()); 
    }
    private void UseSaw() { gunManager.doubleDamage = true; }
    private void UseCuff() { gunManager.isCuffed = true; }

    private void UseSoda() {
        if (gunManager.bulletCount <= 0) return;
        bool isLast = (gunManager.bulletCount == 1);

        // Bỏ đạn hiện tại
        gunManager.bulletCount--; 

        if (isLast) {
            gunManager.isWaitingNextRound = true;
            // Phải đổi lượt để người kia cầm súng ở round sau
            gunManager.ChangeTurn(); 
            
            if (gunManager.player1HP > 0 && gunManager.player2HP > 0) {
                gunManager.currentRound++;
                gunManager.StartCoroutine(gunManager.NextRoundRoutine()); 
            }
        }
    }

    private void UsePill(PlayerRef user)
    {
        int chance = Random.Range(0, 2); 
        ModifyHealth(user, (chance == 0) ? 1 : -1);
    }

    private void UseHealth(PlayerRef user) { ModifyHealth(user, 1); }

    private void ModifyHealth(PlayerRef user, int amount)
    {
        if (gunManager.activePlayerIndex == 0) 
            gunManager.player1HP = Mathf.Clamp(gunManager.player1HP + amount, 0, gunManager.maxHP);
        else 
            gunManager.player2HP = Mathf.Clamp(gunManager.player2HP + amount, 0, gunManager.maxHP);

        // Cập nhật máu qua RPC để chạy animation trên tất cả máy
        gunManager.RPC_AnimateHealth(gunManager.player1HP, gunManager.player2HP, false, false);
    }
}