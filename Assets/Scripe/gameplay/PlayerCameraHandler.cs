using Fusion;
using UnityEngine;

public class PlayerCameraHandler : NetworkBehaviour
{
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_AssignCamera(int sideIndex)
    {
        // Chỉ chạy trên máy của người chơi đó
        if (!HasInputAuthority) return;

        // Tìm tất cả các "Camera Slot" trên Map
        // Ông nên đặt tên 2 cái Camera trên Map là "Cam_0" và "Cam_1"
        GameObject targetCam = GameObject.Find("Cam_" + sideIndex);

        if (targetCam != null)
        {
            // Tắt Main Camera mặc định đi (nếu có)
            if (Camera.main != null && Camera.main.gameObject != targetCam)
            {
                Camera.main.gameObject.SetActive(false);
            }

            // Bật Camera cố định của bên mình lên
            Camera myCam = targetCam.GetComponent<Camera>();
            myCam.enabled = true;
            targetCam.tag = "MainCamera"; // Gán lại tag để các script khác tìm thấy
            
            Debug.Log($"Đã kết nối với Camera bên: {sideIndex}");
        }
    }
}