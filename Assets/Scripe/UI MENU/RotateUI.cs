using UnityEngine;

public class RotateUI : MonoBehaviour
{
    public float rotateSpeed = -200f; // Tốc độ xoay (âm là xoay cùng chiều kim đồng hồ)

    void Update()
    {
        // Xoay trục Z của RectTransform mỗi frame
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
}