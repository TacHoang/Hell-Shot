using Fusion;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpawnPlayersGameplay : MonoBehaviour
{
    private NetworkRunner _runner;
    public NetworkPrefabRef[] playerPrefabs;
    
    [Header("--- Offsets Cho Người Chơi 1 (Bên Trái/Host) ---")]
    public Vector3[] offsetsLeft = new Vector3[4]; 

    [Header("--- Offsets Cho Người Chơi 2 (Bên Phải/Client) ---")]
    public Vector3[] offsetsRight = new Vector3[4]; 

    private List<NetworkObject> _spawnedObjects = new List<NetworkObject>();

    IEnumerator Start()
    {
        // 1. Đợi Runner
        while (_runner == null)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            yield return null;
        }

        // 2. Chỉ Host chạy
        while (!_runner.IsServer && !_runner.IsSharedModeMasterClient)
            yield return null;

        // 3. Đợi đủ player
        while (_runner.ActivePlayers.Count() < 2)
            yield return new WaitForSeconds(0.3f);

        // 4. Đợi RoomPlayer sync
        while (Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None).Length < 2)
            yield return new WaitForSeconds(0.2f);

        yield return new WaitForSeconds(0.5f);

        SpawnAll();
    }

    void SpawnAll()
    {
        if (!_runner.IsServer) return;

        var players = _runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        Vector3 rootPos = transform.position;

        for (int i = 0; i < players.Count; i++)
        {
            int charIndex = GetCharacterIndex(players[i]);

            Vector3 offset = (i == 0) ? offsetsLeft[charIndex] : offsetsRight[charIndex];
            Vector3 spawnPos = rootPos + offset;

            Quaternion rot = transform.rotation * 
                (i == 1 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);

            var obj = _runner.Spawn(playerPrefabs[charIndex], spawnPos, rot, players[i]);
            _spawnedObjects.Add(obj);

            // 🔥 FIX QUAN TRỌNG: GÁN PlayerIndex NGAY SAU SPAWN
            var controller = obj.GetComponent<PlayerActionController>();
            if (controller != null)
            {
                controller.PlayerIndex = i; // 0 = trái, 1 = phải
            }

            // Fix CharacterController
            var cc = obj.GetComponent<CharacterController>();
            if (cc != null)
                StartCoroutine(ActivateCC(cc, spawnPos));

            // Camera
            var cam = obj.GetComponent<PlayerCameraHandler>();
            if (cam != null)
                cam.RPC_AssignCamera(i);
        }

        StartCoroutine(StartGameAfterSpawn());
    }

    IEnumerator StartGameAfterSpawn()
    {
        // Đợi network sync xong
        yield return new WaitForSeconds(0.3f);

        var gun = FindFirstObjectByType<GunManager>();
        if (gun != null)
        {
            gun.canStartSequence = true;
            Debug.Log("<color=green>Game Start!</color>");
        }
    }

    IEnumerator ActivateCC(CharacterController cc, Vector3 pos)
    {
        cc.enabled = false;
        yield return new WaitForSeconds(0.05f);
        cc.transform.position = pos;
        cc.enabled = true;
    }

    int GetCharacterIndex(PlayerRef player)
    {
        var allRP = Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
        var rp = allRP.FirstOrDefault(x => x.Object != null && x.Object.InputAuthority == player);
        return (rp != null) ? rp.CharacterIndex : 0;
    }
}