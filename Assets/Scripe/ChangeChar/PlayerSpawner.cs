using Fusion;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour, IPlayerJoined
{
    public NetworkPrefabRef[] characterPrefabs; // 4 prefab nhân vật
    public Transform[] spawnPoints;             // spawn0 → spawn3

    private NetworkRunner runner;

    void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
            Debug.LogError("❌ Không tìm thấy NetworkRunner!");
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (!runner.IsServer) return;

        // lấy RoomPlayer của player
        var rp = runner.GetPlayerObject(player)?.GetComponent<RoomPlayer>();
        if (rp == null) return;

        int charIndex = Mathf.Clamp(rp.CharacterIndex, 0, characterPrefabs.Length - 1);

        // gán spawn point dựa vào thứ tự join
        List<PlayerRef> players = runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        int playerIndex = players.IndexOf(player);
        Transform spawnPoint = spawnPoints[playerIndex % spawnPoints.Length];

        Quaternion rot = (spawnPoint == spawnPoints[0]) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        NetworkObject playerObj = runner.Spawn(
            characterPrefabs[charIndex],
            spawnPoint.position,
            rot,
            player
        );

        runner.SetPlayerObject(player, playerObj);

        Debug.Log($"Spawn Player {playerIndex} | CharIndex: {charIndex} | Pos: {spawnPoint.position}");
    }
}