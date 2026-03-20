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
            PlayerName = PlayerInfo.Instance.PlayerName;
        }

        Invoke(nameof(UpdateUI), 0.2f);
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
        if (RoomUI.Instance != null)
        {
            RoomUI.Instance.RefreshPlayers();
        }
    }
}