using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public int CharacterIndex { get; set; } = 0;

    public override void Spawned()
    {
        // Chỉ máy của người chơi đó mới thực hiện việc gửi dữ liệu của chính mình lên Server
        if (HasInputAuthority)
        {
            if (PlayerInfo.Instance != null)
            {
                // 🔥 SỬA TẠI ĐÂY: Lấy index đã chọn từ PlayerInfo ở ngoài Menu
                int mySelectedChar = PlayerInfo.Instance.SelectedCharacterIndex;
                RPC_SetCharacter(mySelectedChar); 

                // Lấy tên từ PlayerInfo
                RPC_SetName(PlayerInfo.Instance.PlayerName);
                
                Debug.Log($"[RoomPlayer] Đã đồng bộ nhân vật số {mySelectedChar} từ Menu lên Server.");
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetCharacter(int index)
    {
        // Server nhận lệnh và cập nhật vào biến [Networked] để mọi người cùng thấy
        CharacterIndex = Mathf.Clamp(index, 0, 3);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetName(string name)
    {
        PlayerName = name;
    }

    // Đảm bảo RoomPlayer không bị mất khi chuyển Scene sang trận đấu
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}