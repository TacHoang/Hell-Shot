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
    private void UseSoda() 
    {
        if (!HasStateAuthority) return; 

        bool isReal = gunManager.GetCurrentBulletType();
        
        // 1. Host tự chạy hiệu ứng trên máy mình (Chỉ chạy 1 lần local)
        gunManager.PlaySodaVisualLocal(isReal);

        // 2. Gửi RPC nhưng CHỈ cho máy đối thủ (Proxies). 
        // Máy Host sẽ KHÔNG nhận lại cái này nên không bị văng viên thứ 2.
        gunManager.RPC_AnimateSodaEject_Proxies(isReal); 

        // 3. Trừ dữ liệu đạn (Chỉ xử lý biến số, không sinh Prefab trong này)
        gunManager.EjectBullet(); 

        if (gunManager.bulletCount == 0) {
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
        if (!HasStateAuthority) return; // CHỈ Host mới được tính toán

        // Tính toán may rủi duy nhất 1 lần trên Host
        int chance = Random.Range(0, 2); 
        int amount = (chance == 0) ? 1 : -1;

        // Thay đổi máu trực tiếp (Hàm này đã có check HasStateAuthority của ông rồi)
        ModifyHealth(user, amount);
        
        // Nếu có hiệu ứng âm thanh/hình ảnh uống thuốc, gọi RPC Proxies ở đây
        // gunManager.RPC_PlayPillEffect(user, amount); 
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