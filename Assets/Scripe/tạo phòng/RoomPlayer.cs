using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    [Networked]
    public NetworkString<_32> PlayerName { get; set; }

    [Networked]
    public int CharacterIndex { get; set; } = -1; // 🔥 fix

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // gửi dữ liệu lên server
            RPC_SetName(PlayerInfo.Instance.PlayerName);
            RPC_SetCharacter(CharacterSelection.Instance.characterIndex);
        }

        Invoke(nameof(UpdateUI), 0.3f);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetName(string name)
    {
        PlayerName = name;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetCharacter(int index)
    {
        CharacterIndex = index;
    }

    void UpdateUI()
    {
        if (RoomUI.Instance != null)
        {
            RoomUI.Instance.RefreshPlayers();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
    }
}