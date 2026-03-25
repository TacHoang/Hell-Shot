using Fusion;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class NetworkManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject joinRoomPanel;
    public GameObject loadingPanel; // 🔥 Kéo Panel Loading vào đây
    public GameObject lobbyCanvas;

    [Header("UI Inputs")]
    public TMP_InputField joinRoomInput;

    [Header("Network Settings")]
    public NetworkRunner runnerPrefab;
    private NetworkRunner runner;
    public NetworkPrefabRef roomPlayerPrefab;

    public async void StartGame(GameMode mode, string sessionID)
    {
        // 1. Hiện loading ngay lập tức để chặn người dùng bấm lung tung
        if (loadingPanel != null) loadingPanel.SetActive(true);

        if (runner == null)
        {
            runner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(runner.gameObject);
        }

        runner.ProvideInput = (mode == GameMode.Client);

        var props = new Dictionary<string, SessionProperty>();
        if (mode == GameMode.Host)
            props["HostName"] = PlayerInfo.Instance.PlayerName;

        // Bắt đầu quá trình StartGame của Fusion
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionID,
            SessionProperties = props,
            PlayerCount = 2,
            SceneManager = null // Chạy trong cùng Scene, dùng Canvas để ẩn hiện
        });

        if (result.Ok)
        {
            Debug.Log($"{mode} started successfully. RoomID: {sessionID}");
            
            // Nếu là Host, chuyển sang Lobby luôn vì Host hiếm khi fail
            if (mode == GameMode.Host)
            {
                OnConnectSuccess();
            }
            // Nếu là Client, Coroutine CheckJoinSuccess sẽ lo phần còn lại
        }
        else
        {
            // Nếu fail ngay từ đầu (ví dụ: lỗi mạng local)
            HandleJoinFailed($"Lỗi khởi tạo: {result.ShutdownReason}");
        }
    }

    private void OnConnectSuccess()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);

        if (lobbyCanvas != null)
            lobbyCanvas.SetActive(true);

        // Bắt đầu kiểm tra để spawn nhân vật đại diện trong Lobby
        InvokeRepeating(nameof(CheckSpawnPlayers), 1f, 1f);
    }

    private async void HandleJoinFailed(string reason)
    {
        Debug.LogWarning($"Join thất bại: {reason}");

        if (loadingPanel != null) loadingPanel.SetActive(false);

        // Quan trọng: Phải tắt và xóa Runner cũ để lần sau bấm lại không bị lỗi
        if (runner != null)
        {
            await runner.Shutdown();
            if (runner != null) Destroy(runner.gameObject);
            runner = null;
        }

        // Tắt loading nma vẫn giữ ở màn hình Join để người dùng nhập lại ID
        if (joinRoomPanel != null) joinRoomPanel.SetActive(true);
        
        // Dừng việc check spawn nếu có
        CancelInvoke(nameof(CheckSpawnPlayers));
    }

    IEnumerator CheckJoinSuccess()
    {
        float timeout = 10f; 
        float timer = 0f;

        // Đợi Runner kết nối thành công hoặc bị lỗi/timeout
        while (runner != null && !runner.IsRunning)
        {
            timer += Time.deltaTime;

            if (timer >= timeout)
            {
                HandleJoinFailed("Không tìm thấy phòng hoặc quá thời gian kết nối!");
                yield break;
            }

            yield return null;
        }

        if (runner != null && runner.IsRunning)
        {
            Debug.Log("✅ Join thành công!");
            OnConnectSuccess();
        }
        else
        {
            HandleJoinFailed("Kết nối thất bại!");
        }
    }

    // --- UI Button Events ---

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

        StartGame(GameMode.Client, id);
        StartCoroutine(CheckJoinSuccess());
    }

    // --- Logic Player Management ---

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
                Debug.Log("Spawn player trong lobby: " + player);
                runner.Spawn(roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);
            }
        }

        RemoveDisconnectedPlayers();
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

            if (!stillInGame)
            {
                Debug.Log("Despawn player đã thoát: " + obj.Object.InputAuthority);
                runner.Despawn(obj.Object);
            }
        }
    }
}