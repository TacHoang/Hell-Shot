using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnNameChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            RPC_SetPlayerName(PlayerInfo.Instance.PlayerName);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(NetworkString<_32> name)
    {
        PlayerName = name;
    }

    public void OnNameChanged()
    {
        string newName = PlayerName.ToString();
        if (RoomUI.Instance != null && !string.IsNullOrEmpty(newName))
        {
            RoomUI.Instance.AddPlayer(newName);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (RoomUI.Instance != null && PlayerName.Length > 0)
        {
            RoomUI.Instance.RemovePlayer(PlayerName.ToString());
        }
    }
}