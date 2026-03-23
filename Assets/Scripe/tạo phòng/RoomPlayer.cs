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
            RPC_SetCharacter(CharacterIndex); // CharacterIndex local của player
            if (PlayerInfo.Instance != null)
                RPC_SetName(PlayerInfo.Instance.PlayerName);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetCharacter(int index)
    {
        CharacterIndex = Mathf.Clamp(index, 0, 3);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetName(string name)
    {
        PlayerName = name;
    }
}