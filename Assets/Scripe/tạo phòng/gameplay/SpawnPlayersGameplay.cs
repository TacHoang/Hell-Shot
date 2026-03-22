using UnityEngine;
using Fusion;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;

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
    {
        runner = FindObjectOfType<NetworkRunner>();
    }

    // 🔥 AUTO FIND spawn
    if (spawnLeft == null)
        spawnLeft = GameObject.Find("SpawnLeft")?.transform;

    if (spawnRight == null)
        spawnRight = GameObject.Find("SpawnRight")?.transform;

    Debug.Log("Left: " + (spawnLeft != null ? spawnLeft.position.ToString() : "NULL"));
Debug.Log("Right: " + (spawnRight != null ? spawnRight.position.ToString() : "NULL"));

    StartCoroutine(SpawnGameplayAfterLoad());
}

    IEnumerator SpawnGameplayAfterLoad()
    {
        if (hasSpawned) yield break;
        hasSpawned = true;

        // ⏳ đợi runner + server
        while (runner == null || !runner.IsServer)
            yield return null;

        // ⏳ đợi đúng scene
        while (SceneManager.GetActiveScene().name != gameplaySceneName)
            yield return null;

        yield return new WaitForSeconds(0.5f);

        // ⏳ đợi đủ 2 player thật
        while (runner.ActivePlayers.Count() < 2)
            yield return null;

        // 🔥 LẤY PLAYER CHUẨN
        var players = runner.ActivePlayers.ToList();

        // 🔥 SORT cho ổn định
        players = players.OrderBy(p => p.RawEncoded).ToList();

        if (spawnLeft == null || spawnRight == null)
        {
            Debug.LogError("Chưa gán spawn point!");
            yield break;
        }

        int firstSide = runner.Tick % 2;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];

            int side = (i == 0) ? firstSide : 1 - firstSide;

            Transform spawnPoint = (side == 0) ? spawnLeft : spawnRight;

            Quaternion rot = (side == 0)
                ? Quaternion.Euler(0, 180, 0)
                : Quaternion.identity;

            // 🔥 lấy character từ RoomPlayer
            var rp = FindObjectsOfType<RoomPlayer>()
                .FirstOrDefault(x => x.Object != null && x.Object.InputAuthority == player);

            int charIndex = (rp != null) ? rp.CharacterIndex : 0;

            if (charIndex < 0 || charIndex >= playerPrefabs.Length)
                charIndex = 0;

            Debug.Log($"Spawn Player {player} - Char: {charIndex} - Pos: {spawnPoint.position}");

            runner.Spawn(
                playerPrefabs[charIndex],
                spawnPoint.position,
                rot,
                player
            );
        }
    }
}