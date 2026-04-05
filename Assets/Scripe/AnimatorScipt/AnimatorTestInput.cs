using UnityEngine;

public class AnimatorTestInput : MonoBehaviour
{
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Nhấn phím A để gọi trigger UseLeft
        if (Input.GetKeyDown(KeyCode.A))
        {
            anim.SetTrigger("UseLeft");
            Debug.Log("Trigger UseLeft đã được gọi");
        }

        // Nhấn phím D để gọi trigger UseRight
        if (Input.GetKeyDown(KeyCode.D))
        {
            anim.SetTrigger("UseRight");
            Debug.Log("Trigger UseRight đã được gọi");
        }
    }
}
