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

    private List<NetworkObject> _spawnedTestObjects = new List<NetworkObject>();

    IEnumerator Start()
    {
        // Đợi NetworkRunner khởi động
        while (_runner == null) {
            _runner = FindFirstObjectByType<NetworkRunner>();
            yield return null;
        }

        // Chỉ Host mới có quyền Spawn
        while (!_runner.IsServer && !_runner.IsSharedModeMasterClient) yield return null;

        // Đợi đủ 2 người chơi vào phòng
        while (_runner.ActivePlayers.Count() < 2) yield return new WaitForSeconds(0.5f); 

        // Đợi dữ liệu RoomPlayer (như CharacterIndex) sẵn sàng
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
        // Sắp xếp người chơi để Host luôn đứng đầu (index 0)
        var players = _runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        Vector3 rootPos = transform.position;

        for (int i = 0; i < players.Count; i++)
        {
            int characterIndex = GetCharacterIndex(players[i]);
            
            // i == 0 là Host (Bên trái), i == 1 là Client (Bên phải)
            Vector3 offset = (i == 0) ? offsetsLeft[characterIndex] : offsetsRight[characterIndex];
            
            Vector3 finalPos = rootPos + offset;
            
            // Player 2 (Bên phải) sẽ quay mặt 180 độ đối diện Player 1
            Quaternion rot = transform.rotation * (i == 1 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);

            // Spawn nhân vật
            var playerObj = _runner.Spawn(playerPrefabs[characterIndex], finalPos, rot, players[i]);
            _spawnedTestObjects.Add(playerObj);

            // Bật CharacterController sau khi spawn để tránh lỗi dịch chuyển (Teleport)
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) StartCoroutine(ActivateCC(cc, finalPos));
            
            // Gán Camera cho đúng góc nhìn của mỗi người
            var camHandler = playerObj.GetComponent<PlayerCameraHandler>();
            if (camHandler != null) camHandler.RPC_AssignCamera(i); 
        }
    }

    IEnumerator ActivateCC(CharacterController cc, Vector3 pos) {
        // Tắt CC tạm thời, dịch chuyển về đúng chỗ rồi mới bật lại
        cc.enabled = false; 
        yield return new WaitForSeconds(0.1f); 
        cc.transform.position = pos; 
        cc.enabled = true; 
    }

    int GetCharacterIndex(PlayerRef rel) {
        var allRP = Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
        // Tìm RoomPlayer có InputAuthority khớp với Player đang xét
        var rp = allRP.FirstOrDefault(x => x.Object != null && x.Object.InputAuthority == rel);
        return (rp != null) ? rp.CharacterIndex : 0;
    }
}