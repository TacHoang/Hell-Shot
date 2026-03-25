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
    public GameObject loadingPanel; // 🔥 Nơi chứa icon xoay xoay
    public GameObject lobbyCanvas;

    [Header("UI Inputs")]
    public TMP_InputField joinRoomInput;

    [Header("Network Settings")]
    public NetworkRunner runnerPrefab;
    private NetworkRunner runner;
    public NetworkPrefabRef roomPlayerPrefab;

    // --- KHỞI TẠO VÀ KẾT NỐI ---

    public async void StartGame(GameMode mode, string sessionID)
    {
        // 1. Hiện loading ngay lập tức
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

        // Bắt đầu StartGame
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionID,
            SessionProperties = props,
            PlayerCount = 2,
            SceneManager = null 
        });

        if (result.Ok)
        {
            Debug.Log($"{mode} started successfully. RoomID: {sessionID}");
            
            if (mode == GameMode.Host)
            {
                OnConnectSuccess();
            }
            // Nếu Client, CheckJoinSuccess sẽ lo tiếp
        }
        else
        {
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

        InvokeRepeating(nameof(CheckSpawnPlayers), 1f, 1f);
    }

    // --- DỌN DẸP VÀ THOÁT ---

    // Hàm gọi khi nhấn nút Back từ trong Lobby (Phòng)
    public async void LeaveLobby()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);

        CancelInvoke(nameof(CheckSpawnPlayers));

        if (runner != null)
        {
            // Shutdown để đóng session trên Cloud
            await runner.Shutdown();
            if (runner != null) Destroy(runner.gameObject);
            runner = null;
        }

        // Quay lại trạng thái trước khi tạo host/join (Main Menu)
        if (lobbyCanvas != null) lobbyCanvas.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        Debug.Log("Đã thoát phòng và dọn dẹp Runner.");
    }

    private async void HandleJoinFailed(string reason)
    {
        Debug.LogWarning($"Join thất bại: {reason}");

        if (loadingPanel != null) loadingPanel.SetActive(false);

        if (runner != null)
        {
            await runner.Shutdown();
            if (runner != null) Destroy(runner.gameObject);
            runner = null;
        }

        if (joinRoomPanel != null) joinRoomPanel.SetActive(true);
        
        CancelInvoke(nameof(CheckSpawnPlayers));
    }

    // Dọn rác khi người chơi tắt game đột ngột (Alt+F4)
    private void OnApplicationQuit()
    {
        if (runner != null) runner.Shutdown();
    }

    IEnumerator CheckJoinSuccess()
    {
        float timeout = 10f; 
        float timer = 0f;

        while (runner != null && !runner.IsRunning)
        {
            timer += Time.deltaTime;
            if (timer >= timeout)
            {
                HandleJoinFailed("Không tìm thấy phòng hoặc timeout!");
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

    // --- UI BUTTON EVENTS ---

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
        // Nếu người dùng đang ở màn hình nhập ID rồi bấm Back
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

    // --- LOGIC PLAYER MANAGEMENT ---

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
                Debug.Log("Spawn player lobby: " + player);
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
                Debug.Log("Xóa rác player thoát: " + obj.Object.InputAuthority);
                runner.Despawn(obj.Object);
            }
        }
    }
}