using UnityEngine;
using TMPro;
using Fusion;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;

public class RoomUI : MonoBehaviour
{
    public static RoomUI Instance;

    public TextMeshProUGUI roomIDText;
    public TextMeshProUGUI roomNameText;
    public GameObject startGameButton;

    public TextMeshProUGUI playerSlot1;
    public TextMeshProUGUI playerSlot2;

    public SceneRef gameplayScene;

    private NetworkRunner runner;
    private bool hasLeft = false; // 👈 chống load nhiều lần

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null && runner.SessionInfo.IsValid)
        {
            roomIDText.text = "ID: " + runner.SessionInfo.Name;
            startGameButton.SetActive(runner.IsServer);

            if (runner.SessionInfo.Properties.TryGetValue("HostName", out var hostName))
            {
                roomNameText.text = "Phòng của: " + hostName;
            }
            else
            {
                roomNameText.text = "Phòng đang tải...";
            }
        }

        InvokeRepeating(nameof(RefreshPlayers), 0.5f, 1f);
    }

    private float connectionLifeTime = 0f; // Thêm biến này ở trên đầu class RoomUI

    void Update()
    {
        if (runner == null || hasLeft) return;

        // 1. Tăng thời gian đếm để biết script đã chạy được bao lâu
        connectionLifeTime += Time.deltaTime;

        // 2. 🔥 QUAN TRỌNG: Nếu chưa ở trong phòng quá 3 giây, ĐỪNG CHECK GÌ CẢ
        // Mục đích: Đợi Fusion đồng bộ xong danh sách người chơi (ActivePlayers)
        if (connectionLifeTime < 3f) return;

        // 3. Cứ mỗi 30 frame (khoảng 0.5 giây) mới kiểm tra 1 lần để nhẹ máy
        if (Time.frameCount % 30 == 0) 
        {
            // 👉 CLIENT tự detect host thoát
            if (!runner.IsServer)
            {
                // Nếu danh sách chỉ còn 1 người (là chính mình) hoặc Runner đã ngừng chạy
                if (runner.ActivePlayers.Count() <= 1 || !runner.IsRunning)
                {
                    hasLeft = true;
                    Debug.Log("Host đã thoát hoặc mất kết nối → quay về menu");
                    
                    // Gọi hàm dọn dẹp sạch sẽ của bạn
                    StartCoroutine(FullResetGame());
                }
            }
        }
    }

    public void RefreshPlayers()
    {
        var players = FindObjectsOfType<RoomPlayer>();

        // 🔥 FIX NULL CRASH
        players = System.Array.FindAll(players, p =>
            p != null &&
            p.Object != null &&
            p.Object.InputAuthority != null
        );

        if (players.Length > 1)
        {
            System.Array.Sort(players, (a, b) =>
                a.Object.InputAuthority.RawEncoded.CompareTo(b.Object.InputAuthority.RawEncoded)
            );
        }

        playerSlot1.text = "";
        playerSlot2.text = "";

        if (players.Length > 0)
        {
            string name = players[0].PlayerName.ToString();
            playerSlot1.text = string.IsNullOrEmpty(name) ? "..." : name;
        }

        if (players.Length > 1)
        {
            string name = players[1].PlayerName.ToString();
            playerSlot2.text = string.IsNullOrEmpty(name) ? "..." : name;
        }
    }

    public void OnClickStartGame()
    {
        if (runner != null && runner.IsServer)
        {
            // 1. Khóa Room để tránh người lạ vào khi đang load
            runner.SessionInfo.IsOpen = false;
            runner.SessionInfo.IsVisible = false;

            // 2. Dùng runner.LoadScene thay vì SceneManager.LoadScene
            // Lệnh này sẽ yêu cầu TẤT CẢ Client trong phòng cùng load sang scene mới
            runner.LoadScene(gameplayScene);
            
            Debug.Log("Host đã ra lệnh chuyển sang Scene Gameplay!");
        }
    }

    // ================== LEAVE ==================

    public void OnClickLeave()
    {
        if (!hasLeft)
        {
            hasLeft = true;
            StartCoroutine(FullResetGame());
        }
    }

    public IEnumerator FullResetGame()
    {
        // 1. Tắt tất cả NetworkRunner đang chạy
        var runners = FindObjectsOfType<NetworkRunner>();
        foreach (var r in runners)
        {
            if (r != null && r.IsRunning)
                r.Shutdown(true); // force client/server leave
        }

        // 2. Chờ một lúc để Fusion xử lý xong shutdown
        yield return new WaitForSeconds(1f);

        // 3. Xóa hết runner cũ
        foreach (var r in runners)
        {
            if (r != null)
                Destroy(r.gameObject);
        }

        // 4. Reset tất cả singleton và biến static
        if (PlayerInfo.Instance != null)
        {
            PlayerInfo.Instance.PlayerName = "";
            PlayerInfo.Instance = null; // reset hoàn toàn
        }

        RoomUI.Instance = null;

        // Nếu bạn có callback holder riêng, reset luôn
        // NetworkRunnerCallbacksHolder.Instance = null;

        // 5. Load lại scene đầu tiên → tất cả Awake/Start sẽ chạy lại
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}