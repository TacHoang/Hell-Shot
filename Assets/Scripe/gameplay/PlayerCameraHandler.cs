using Fusion;
using UnityEngine;

public class PlayerCameraHandler : NetworkBehaviour
{
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_AssignCamera(int sideIndex)
    {
        // Chỉ chạy trên máy của người chơi đó (thằng đang cầm chuột)
        if (!HasInputAuthority) return;

        // 1. Tìm Camera của bên mình ("Cam_0" hoặc "Cam_1")
        GameObject targetCam = GameObject.Find("Cam_" + sideIndex);

        if (targetCam != null)
        {
            // 2. Tắt Main Camera mặc định đi (nếu có)
            if (Camera.main != null && Camera.main.gameObject != targetCam)
            {
                Camera.main.gameObject.SetActive(false);
            }

            // 3. Bật Camera cố định của bên mình lên
            Camera myCam = targetCam.GetComponent<Camera>();
            if (myCam != null)
            {
                myCam.enabled = true;
                targetCam.tag = "MainCamera"; // Gán lại tag để Unity dễ tìm
                
                // --- ĐOẠN SỬA ĐỂ BẤM ĐƯỢC NÚT TRÊN BÀN ---
                
                // Tìm TẤT CẢ các Canvas có trong Scene
                Canvas[] allCanvases = GameObject.FindObjectsOfType<Canvas>();
                
                foreach (Canvas c in allCanvases)
                {
                    // Chỉ xử lý những Canvas nào đang để chế độ WorldSpace (nằm trên bàn)
                    if (c.renderMode == RenderMode.WorldSpace)
                    {
                        c.worldCamera = myCam; // Ép cái Canvas đó phải nhận Camera của mình
                        Debug.Log($"Đã gán Camera {targetCam.name} vào Canvas: {c.name}");
                    }
                }
                // ------------------------------------------
            }
            
            Debug.Log($"Đã kết nối hoàn tất với Camera bên: {sideIndex}");
        }
        else
        {
            Debug.LogError($"KHÔNG TÌM THẤY Camera tên: Cam_{sideIndex} trên Map!");
        }
    }
}