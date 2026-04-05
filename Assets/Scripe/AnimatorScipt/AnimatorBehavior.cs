using UnityEngine;

public class AnimatorBehavior : MonoBehaviour
{
    private Animator anim;
    private AnimationEventHandler eventHandler;

    void Awake()
    {
        anim = GetComponent<Animator>();
        eventHandler = GetComponent<AnimationEventHandler>();
    }

    void Update()
    {
        // Ví d?: click chu?t trái sau khi animation k?t thúc
        if (Input.GetMouseButtonDown(0))
        {
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("SitCatchRight3"))
            {
                // Sau khi soi ??n xong, quay l?i Sit
                eventHandler.OnReturnToSit();
            }
        }
    }
}
