using Fusion;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpawnPlayersGameplay : MonoBehaviour
{
    private NetworkRunner _runner;
    public NetworkPrefabRef[] playerPrefabs;
    public Transform spawnLeft;
    public Transform spawnRight;

    IEnumerator Start()
    {
        while (_runner == null) {
            _runner = FindFirstObjectByType<NetworkRunner>();
            yield return null;
        }

        while (!_runner.IsServer) yield return null;

        while (_runner.ActivePlayers.Count() < 2) { 
            yield return new WaitForSeconds(0.5f); 
        }

        yield return new WaitForSeconds(0.5f);

        if (spawnLeft != null && spawnRight != null) {
            while (spawnLeft.position == Vector3.zero && spawnRight.position == Vector3.zero) {
                Debug.Log("Đang đợi Transform Spawn cập nhật tọa độ thực...");
                yield return null; 
            }
        }

        yield return new WaitForSeconds(0.2f);

        SpawnAll();
    }

    void SpawnAll()
    {
        var players = _runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        
        for (int i = 0; i < players.Count; i++)
        {
            Transform targetPoint = (i == 0) ? spawnLeft : spawnRight;
            if (targetPoint == null) continue;

            Vector3 pos = targetPoint.position;
            
            // Lấy góc xoay từ Transform trong Unity Editor
            Quaternion rot = targetPoint.rotation;

            // --- ĐOẠN SỬA ĐỂ HAI CON NHÌN NHAU ---
            // Nếu là người chơi thứ 2 (bên phải), xoay thêm 180 độ quanh trục Y
            if (i == 1) {
                rot *= Quaternion.Euler(0, 180, 0);
            }
            // ------------------------------------

            int characterIndex = GetCharacterIndex(players[i]);

            var playerObj = _runner.Spawn(
                playerPrefabs[characterIndex], 
                pos, 
                rot, 
                players[i]
            );

            // Cưỡng chế vị trí và góc xoay ngay lập tức
            playerObj.transform.position = pos;
            playerObj.transform.rotation = rot;

            if (playerObj.TryGetComponent<NetworkTransform>(out var nt)) {
                nt.Teleport(pos, rot);
            }
            
            if (playerObj.TryGetComponent<CharacterController>(out var cc)) {
                cc.enabled = false;
                StartCoroutine(ReEnableCC(cc));
            }
        }
    }

    int GetCharacterIndex(PlayerRef rel) 
    {
        var allRoomPlayers = Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
        var rp = allRoomPlayers.FirstOrDefault(x => x.Object != null && x.Object.InputAuthority == rel);
        return (rp != null) ? rp.CharacterIndex : 0;
    }

    IEnumerator ReEnableCC(CharacterController cc) {
        yield return new WaitForSeconds(0.1f);
        if (cc != null) cc.enabled = true;
    }
}