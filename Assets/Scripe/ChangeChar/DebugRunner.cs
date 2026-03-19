using Fusion;
using UnityEngine;

public class DebugRunner : MonoBehaviour
{
    void Start()
    {
        var runner = FindObjectOfType<NetworkRunner>();

        if (runner == null)
        {
            Debug.LogError("❌ KHÔNG có NetworkRunner trong scene");
        }
        else
        {
            Debug.Log("✅ Runner tồn tại");
            Debug.Log("IsRunning: " + runner.IsRunning);
        }
    }
}