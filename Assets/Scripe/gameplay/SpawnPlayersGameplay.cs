using Fusion;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpawnPlayersGameplay : MonoBehaviour
{
    private NetworkRunner _runner;
    public NetworkPrefabRef[] playerPrefabs;
    
    [Header("--- Offsets Cho Người Chơi 1 (Bên Trái) ---")]
    public Vector3[] offsetsLeft = new Vector3[4]; 

    [Header("--- Offsets Cho Người Chơi 2 (Bên Phải) ---")]
    public Vector3[] offsetsRight = new Vector3[4]; 

    private List<NetworkObject> _spawnedTestObjects = new List<NetworkObject>();

    IEnumerator Start()
    {
        while (_runner == null) {
            _runner = FindFirstObjectByType<NetworkRunner>();
            yield return null;
        }

        while (!_runner.IsServer && !_runner.IsSharedModeMasterClient) yield return null;

        while (_runner.ActivePlayers.Count() < 2) yield return new WaitForSeconds(0.5f); 

        int readyPlayers = 0;
        while (readyPlayers < 2) {
            var allRP = Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
            readyPlayers = allRP.Length;
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.5f);
        SpawnAll();
    }

    public void Test_NextCharacterAndSpawn()
    {
        if (_runner == null || !_runner.IsServer) return;

        var allRP = Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
        foreach (var rp in allRP) {
            int next = rp.CharacterIndex + 1;
            if (next >= playerPrefabs.Length) next = 0;
            rp.CharacterIndex = next; 
        }

        foreach (var obj in _spawnedTestObjects) {
            if (obj != null) _runner.Despawn(obj);
        }
        _spawnedTestObjects.Clear();

        SpawnAll();
    }

    void SpawnAll()
        {
            var players = _runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
            Vector3 rootPos = transform.position;

            for (int i = 0; i < players.Count; i++)
            {
                int characterIndex = GetCharacterIndex(players[i]);
                Vector3 offset = (i == 0) ? offsetsLeft[characterIndex] : offsetsRight[characterIndex];
                
                Vector3 finalPos = rootPos + offset;
                Quaternion rot = transform.rotation * (i == 1 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);

                // 1. Spawn THẲNG vào vị trí chuẩn
                var playerObj = _runner.Spawn(playerPrefabs[characterIndex], finalPos, rot, players[i]);
                _spawnedTestObjects.Add(playerObj);

                // 2. Chỉ cần một bước nhỏ để bật lại "linh hồn" cho nó
                CharacterController cc = playerObj.GetComponent<CharacterController>();
                if (cc != null) StartCoroutine(ActivateCC(cc, finalPos));
                
                // Camera setup giữ nguyên
                var camHandler = playerObj.GetComponent<PlayerCameraHandler>();
                if (camHandler != null) camHandler.RPC_AssignCamera(i); 
            }
        }

        IEnumerator ActivateCC(CharacterController cc, Vector3 pos) {
            yield return new WaitForSeconds(0.1f); // Đợi Fusion ổn định
            cc.transform.position = pos; // Ép lại lần cuối cho chắc
            cc.enabled = true; 
        }

    int GetCharacterIndex(PlayerRef rel) {
        var allRP = Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
        var rp = allRP.FirstOrDefault(x => x.Object != null && x.Object.InputAuthority == rel);
        return (rp != null) ? rp.CharacterIndex : 0;
    }

    IEnumerator ReEnableCC(CharacterController cc, Vector3 fixedPos, Quaternion fixedRot) {
        yield return new WaitForSeconds(0.1f);
        if (cc != null) {
            cc.transform.position = fixedPos;
            cc.enabled = true;
            cc.transform.position = fixedPos; 
        }
    }
}