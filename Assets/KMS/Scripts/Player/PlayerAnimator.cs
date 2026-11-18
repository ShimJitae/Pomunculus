using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
    public static readonly int AttackHash = Animator.StringToHash("Attack");
    public static readonly int DodgeHash = Animator.StringToHash("Dodge");
    public static readonly int HitHash = Animator.StringToHash("Hit");
    public static readonly int DieHash = Animator.StringToHash("Die");
    public static readonly int SpeedParam = Animator.StringToHash("Speed");

    [SerializeField] private float defaultFade = 0.08f;
    [SerializeField] private float speedDampTime = 0.1f;

    private Animator animator;

    private void Awake() => animator = GetComponentInChildren<Animator>();

    public void PlayAnim(int stateHash, bool restart = false)
    {
        if (animator == null) return;   
        if (restart) { animator.Play(stateHash, 0, 0f); return; }

        int playing = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0).shortNameHash
            : animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (playing != stateHash) animator.CrossFadeInFixedTime(stateHash, defaultFade);
    }

    public void SetAnimSpeed(float value)
    {
        if (animator != null) animator.SetFloat(SpeedParam, value, speedDampTime, Time.deltaTime);
    }
}
