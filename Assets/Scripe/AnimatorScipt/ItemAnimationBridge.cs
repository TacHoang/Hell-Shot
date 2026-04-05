using Fusion;
using UnityEngine;

public class ItemAnimationBridge : NetworkBehaviour
{
    private Animator anim;
    public PlayerRef ownerRef;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public override void Spawned()
    {
        ownerRef = Object.InputAuthority; // gán PlayerRef cho bridge
        ItemsManager.RegisterBridge(this); // đăng ký vào danh sách
    }

    public void PlayGlassLeftAnimation() => anim.SetTrigger("UseLeft");
    public void PlayGlassRightAnimation() => anim.SetTrigger("UseRight");
    public void PlaySawAnimation() => anim.SetTrigger("UseSaw");
    public void PlayCuffAnimation() => anim.SetTrigger("UseCuff");
    public void PlaySodaAnimation() => anim.SetTrigger("UseSoda");
    public void PlayPillAnimation() => anim.SetTrigger("UsePill");
    public void PlayHealthAnimation() => anim.SetTrigger("UseHealth");
}
