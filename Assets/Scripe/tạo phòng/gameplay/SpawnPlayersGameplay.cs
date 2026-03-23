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

        // đợi server
        while (runner == null || !runner.IsServer)
            yield return null;

        // đợi đúng scene
        while (SceneManager.GetActiveScene().name != gameplaySceneName)
            yield return null;

        // đợi đủ player
        while (runner.ActivePlayers.Count() < 2)
            yield return null;

        yield return new WaitForSeconds(0.5f);

        // lấy player list ổn định
        List<PlayerRef> players = runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];

            Transform spawnPoint = (i % 2 == 0) ? spawnLeft : spawnRight;
            Quaternion rot = (spawnPoint == spawnLeft) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

            var rp = runner.GetPlayerObject(player)?.GetComponent<RoomPlayer>();
            int charIndex = (rp != null) ? rp.CharacterIndex : 0;
            charIndex = Mathf.Clamp(charIndex, 0, playerPrefabs.Length - 1);

            runner.Spawn(
                playerPrefabs[charIndex],
                spawnPoint.position,
                rot,
                player
            );

            Debug.Log($"Spawn {player} | CharIndex: {charIndex} | Pos: {spawnPoint.position}");
        }
    }
}