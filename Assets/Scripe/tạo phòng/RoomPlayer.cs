using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    [Networked]
    public NetworkString<_32> PlayerName { get; set; }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // 👇 Gửi tên lên server để sync
            RPC_SetName(PlayerInfo.Instance.PlayerName);
        }

        Invoke(nameof(UpdateUI), 0.3f);
    }

    // 👇 RPC: client gửi → server set
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetName(string name)
    {
        PlayerName = name;
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
        // Không làm gì
    }
}