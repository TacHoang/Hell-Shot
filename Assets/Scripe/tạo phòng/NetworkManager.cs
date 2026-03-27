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
    public GameObject loadingPanel; 
    public GameObject lobbyCanvas;

    [Header("UI Inputs")]
    public TMP_InputField joinRoomInput;

    [Header("Network Settings")]
    public NetworkRunner runnerPrefab;
    private NetworkRunner runner;
    public NetworkPrefabRef roomPlayerPrefab;

    // --- LOGIC MỚI: DÙNG COROUTINE ĐỂ HIỆN UI TRƯỚC ---

    public void OnClickCreate()
    {
        string randomID = Random.Range(100000, 999999).ToString();
        // Chạy qua Coroutine để không bị đơ UI
        StartCoroutine(StartGameRoutine(GameMode.Host, randomID));
    }

    public void ConfirmJoin()
    {
        string id = joinRoomInput.text.Trim();
        if (string.IsNullOrEmpty(id)) return;
        StartCoroutine(StartGameRoutine(GameMode.Client, id));
    }

    IEnumerator StartGameRoutine(GameMode mode, string sessionID)
    {
        // 1. Hiện loading ngay lập tức
        if (loadingPanel != null) loadingPanel.SetActive(true);

        // 2. 🔥 QUAN TRỌNG: Đợi cho đến khi frame này được vẽ xong hoàn toàn
        // Điều này đảm bảo cái Loading Panel thực sự hiện lên mắt người dùng trước khi CPU bị đơ
        yield return null;
        yield return new WaitForEndOfFrame();

        // 3. Gọi hàm StartGame (async)
        StartGame(mode, sessionID);

        // 4. Nếu là Client, chạy thêm check thành công
        if (mode == GameMode.Client)
        {
            StartCoroutine(CheckJoinSuccess());
        }
    }

    // --- KHỞI TẠO VÀ KẾT NỐI (Đã tối ưu) ---

    public async void StartGame(GameMode mode, string sessionID)
    {
        if (runner == null)
        {
            runner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(runner.gameObject);
        }

        runner.ProvideInput = (mode == GameMode.Client);

        var props = new Dictionary<string, SessionProperty>();
        try 
        {
            if (mode == GameMode.Host && PlayerInfo.Instance != null)
                props["HostName"] = PlayerInfo.Instance.PlayerName;
        }
        catch { Debug.LogWarning("PlayerInfo.Instance chưa sẵn sàng."); }

        // Bắt đầu chạy Fusion
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionID,
            SessionProperties = props,
            PlayerCount = 2,
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log($"{mode} started successfully. RoomID: {sessionID}");
            if (mode == GameMode.Host) OnConnectSuccess();
        }
        else
        {
            HandleJoinFailed($"Lỗi: {result.ShutdownReason}");
        }
    }

    private void OnConnectSuccess()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);

        if (lobbyCanvas != null)
            lobbyCanvas.SetActive(true);

        CancelInvoke(nameof(CheckSpawnPlayers));
        InvokeRepeating(nameof(CheckSpawnPlayers), 1f, 1f);
    }

    // --- DỌN DẸP VÀ THOÁT ---

    public async void LeaveLobby()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        CancelInvoke(nameof(CheckSpawnPlayers));

        if (runner != null)
        {
            await runner.Shutdown();
            if (runner != null) Destroy(runner.gameObject);
            runner = null;
        }

        if (lobbyCanvas != null) lobbyCanvas.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    private async void HandleJoinFailed(string reason)
    {
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

    IEnumerator CheckJoinSuccess()
    {
        float timeout = 20f; 
        float timer = 0f;

        while (runner == null && timer < timeout) {
            timer += Time.deltaTime;
            yield return null;
        }

        while (runner != null && !runner.IsRunning) {
            timer += Time.deltaTime;
            if (timer >= timeout) {
                HandleJoinFailed("Timeout kết nối!");
                yield break;
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        if (runner != null && runner.IsRunning) OnConnectSuccess();
    }

    // --- UI BUTTON EVENTS ---

    public void OpenJoinRoom()
    {
        if (joinRoomPanel != null) joinRoomPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void BackFromJoin()
    {
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // --- LOGIC PLAYER MANAGEMENT ---

    void CheckSpawnPlayers()
    {
        if (runner == null || !runner.IsServer) return;

        foreach (var player in runner.ActivePlayers)
        {
            bool hasPlayer = false;
            var roomPlayers = Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
            foreach (var obj in roomPlayers)
            {
                if (obj.Object != null && obj.Object.InputAuthority == player)
                {
                    hasPlayer = true;
                    break;
                }
            }

            if (!hasPlayer)
            {
                runner.Spawn(roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);
            }
        }
        RemoveDisconnectedPlayers();
    }

    void RemoveDisconnectedPlayers()
    {
        if (runner == null || !runner.IsServer) return;

        var activePlayers = runner.ActivePlayers;
        var roomPlayers = Object.FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
        foreach (var obj in roomPlayers)
        {
            if (obj.Object == null) continue;

            bool stillInGame = false;
            foreach (var player in activePlayers)
            {
                if (obj.Object.InputAuthority == player) {
                    stillInGame = true;
                    break;
                }
            }

            if (!stillInGame) runner.Despawn(obj.Object);
        }
    }

    private void OnApplicationQuit()
    {
        if (runner != null) runner.Shutdown();
    }
}