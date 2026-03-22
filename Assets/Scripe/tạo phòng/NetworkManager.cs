using Fusion;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject joinRoomPanel;
    public TMP_InputField joinRoomInput;

    public NetworkRunner runnerPrefab;
    private NetworkRunner runner;

    public SceneRef lobbyScene;
    public NetworkPrefabRef roomPlayerPrefab;

    public async void StartGame(GameMode mode, string sessionID)
    {
        if (runner == null)
        {
            runner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(runner.gameObject);
        }

        runner.ProvideInput = (mode == GameMode.Client);

        var props = new Dictionary<string, SessionProperty>();
        if (mode == GameMode.Host)
            props["HostName"] = PlayerInfo.Instance.PlayerName;

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionID,
            SessionProperties = props,
            Scene = lobbyScene,
            PlayerCount = 2,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        mainMenuPanel.SetActive(false);
        joinRoomPanel.SetActive(false);

        Debug.Log($"{mode} started. RoomID: {sessionID}");

        // 🔥 FIX: kiểm tra spawn liên tục
        InvokeRepeating(nameof(CheckSpawnPlayers), 1f, 1f);
    }

    void CheckSpawnPlayers()
    {
        if (runner == null || !runner.IsServer) return;

        foreach (var player in runner.ActivePlayers)
        {
            bool hasPlayer = false;

            foreach (var obj in FindObjectsOfType<RoomPlayer>())
            {
                if (obj.Object != null && obj.Object.InputAuthority == player)
                {
                    hasPlayer = true;
                    break;
                }
            }

            if (!hasPlayer)
            {
                Debug.Log("Spawn player: " + player);

                runner.Spawn(
                    roomPlayerPrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    player
                );
            }
        }

        // 🔥 THÊM DÒNG NÀY
        RemoveDisconnectedPlayers();
    }

    public void OnClickCreate()
    {
        string randomID = Random.Range(100000, 999999).ToString();
        StartGame(GameMode.Host, randomID);
    }

    public void OpenJoinRoom()
    {
        joinRoomPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    public void BackFromJoin()
    {
        joinRoomPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ConfirmJoin()
    {
        string id = joinRoomInput.text.Trim();

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("Nhập ID phòng!");
            return;
        }

        // Start client
        StartGame(GameMode.Client, id);

        // 🔥 Thêm callback kiểm tra join thành công
        StartCoroutine(CheckJoinSuccess());
    }

IEnumerator CheckJoinSuccess()
{
    float timeout = 10f; // 🔥 tăng lên
    float timer = 0f;

    while (runner == null || !runner.IsRunning)
    {
        timer += Time.deltaTime;

        if (timer >= timeout)
        {
            Debug.LogWarning("Không thể join phòng! Quay về menu...");

            if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

            yield break;
        }

        yield return null;
    }

    Debug.Log("✅ Join thành công!");
}

    void RemoveDisconnectedPlayers()
    {
        if (runner == null || !runner.IsServer) return;

        var activePlayers = runner.ActivePlayers;

        foreach (var obj in FindObjectsOfType<RoomPlayer>())
        {
            if (obj.Object == null) continue;

            bool stillInGame = false;

            foreach (var player in activePlayers)
            {
                if (obj.Object.InputAuthority == player)
                {
                    stillInGame = true;
                    break;
                }
            }

            // ❌ nếu player không còn trong room → xóa
            if (!stillInGame)
            {
                Debug.Log("Despawn player: " + obj.Object.InputAuthority);
                runner.Despawn(obj.Object);
            }
        }
    }
}