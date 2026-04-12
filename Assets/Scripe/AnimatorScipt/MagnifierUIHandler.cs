using UnityEngine;

public class MagnifierUIHandler : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject realBulletIcon; // Object chứa hình ảnh/hiệu ứng đạn thật
    public GameObject fakeBulletIcon; // Object chứa hình ảnh/hiệu ứng đạn giả

    public void ShowResult(bool isReal)
    {
        HideUI(); 
        if (isReal) realBulletIcon.SetActive(true);
        else fakeBulletIcon.SetActive(true);

        // Tự động ẩn sau 2 giây cho đỡ rác màn hình
        Invoke(nameof(HideUI), 2.0f);
    }

    public void HideUI()
    {
        if (realBulletIcon) realBulletIcon.SetActive(false);
        if (fakeBulletIcon) fakeBulletIcon.SetActive(false);
    }
    
    // Tự động ẩn khi Object Kính lúp bị tắt
    private void OnDisable() => HideUI();
}