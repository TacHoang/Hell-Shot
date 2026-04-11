using UnityEngine;

public class MagnifierUIHandler : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject realBulletIcon; // Object chứa hình ảnh/hiệu ứng đạn thật
    public GameObject fakeBulletIcon; // Object chứa hình ảnh/hiệu ứng đạn giả

    public void ShowResult(bool isReal)
    {
        HideUI(); // Đảm bảo reset trước khi hiện
        if (isReal) realBulletIcon.SetActive(true);
        else fakeBulletIcon.SetActive(true);
    }

    public void HideUI()
    {
        if (realBulletIcon) realBulletIcon.SetActive(false);
        if (fakeBulletIcon) fakeBulletIcon.SetActive(false);
    }
    
    // Tự động ẩn khi Object Kính lúp bị tắt
    private void OnDisable() => HideUI();
}