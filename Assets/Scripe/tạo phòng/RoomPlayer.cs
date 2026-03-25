using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public int CharacterIndex { get; set; } = 0;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // Ưu tiên lấy từ CharacterSelection vì nó vừa được nhấn nút đổi
            int myIndex = CharacterSelection.Instance != null ? 
                        CharacterSelection.Instance.characterIndex : 0;
            
            RPC_SetCharacter(myIndex); 
            
            if (PlayerInfo.Instance != null)
                RPC_SetName(PlayerInfo.Instance.PlayerName);
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