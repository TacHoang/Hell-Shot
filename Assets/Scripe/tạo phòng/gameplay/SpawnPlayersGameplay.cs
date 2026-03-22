using UnityEngine;
using Fusion;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public class SpawnPlayersGameplay : MonoBehaviour
{
    public NetworkRunner runner;
    public string gameplaySceneName = "Gameplay";

    public NetworkPrefabRef[] playerPrefabs;

    public Transform spawnLeft;
    public Transform spawnRight;

    bool hasSpawned = false;

    void Start()
    {
        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();

        StartCoroutine(SpawnPlayers());
    }

    IEnumerator SpawnPlayers()
    {
        if (hasSpawned) yield break;
        hasSpawned = true;

        // ⏳ đợi server
        while (runner == null || !runner.IsServer)
            yield return null;

        // ⏳ đợi đúng scene
        while (SceneManager.GetActiveScene().name != gameplaySceneName)
            yield return null;

        // ⏳ đợi đủ 2 player
        while (runner.ActivePlayers.Count() < 2)
            yield return null;

        yield return new WaitForSeconds(0.5f);

        // 🔥 lấy player list ổn định
        List<PlayerRef> players = runner.ActivePlayers
            .OrderBy(p => p.RawEncoded)
            .ToList();

        int firstSide = Random.Range(0, 2); // random thật

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];

            int side = (i == 0) ? firstSide : 1 - firstSide;

            Transform spawnPoint = (side == 0) ? spawnLeft : spawnRight;

            Quaternion rot = (side == 0)
                ? Quaternion.Euler(0, 180, 0)
                : Quaternion.identity;

            // 🔥 tìm RoomPlayer để lấy CharacterIndex
            var rp = FindObjectsOfType<RoomPlayer>()
                .FirstOrDefault(x => x.Object != null && x.Object.InputAuthority == player);

            int charIndex = (rp != null) ? rp.CharacterIndex : 0;

            if (charIndex < 0 || charIndex >= playerPrefabs.Length)
                charIndex = 0;

            Debug.Log($"Spawn {player} | Char: {charIndex} | Pos: {spawnPoint.position}");

            runner.Spawn(
                playerPrefabs[charIndex],
                spawnPoint.position,
                rot,
                player
            );
        }
    }
}