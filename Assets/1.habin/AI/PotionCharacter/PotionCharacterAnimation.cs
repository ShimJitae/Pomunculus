using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Animator))]
public class PotionCharacterAnimation : MonoBehaviour
{
    [Tooltip("Preview walk with W / Up Arrow, run with Shift, throw with T, roll with Space, jump with J, and gesture with M.")]
    public bool enableKeyboardPreview = true;
    Animator animator;
    static readonly int GestureParameter = Animator.StringToHash("MiddleFinger");
    static readonly int JumpParameter = Animator.StringToHash("Jump");
    static readonly int RollParameter = Animator.StringToHash("Roll");
    static readonly int RunningParameter = Animator.StringToHash("Running");
    static readonly int WalkingParameter = Animator.StringToHash("Walking");
    static readonly int ThrowParameter = Animator.StringToHash("Throw");
    void Awake() { animator = GetComponent<Animator>(); }
    void Update()
    {
        if (!enableKeyboardPreview) return;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) { bool moving = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed; SetWalking(moving); SetRunning(moving && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)); if (Keyboard.current.mKey.wasPressedThisFrame) PlayGesture(); else if (Keyboard.current.jKey.wasPressedThisFrame) PlayJump(); else if (Keyboard.current.spaceKey.wasPressedThisFrame) PlayRoll(); else if (Keyboard.current.tKey.wasPressedThisFrame) PlayThrow(); }
#elif ENABLE_LEGACY_INPUT_MANAGER
        bool moving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow); SetWalking(moving); SetRunning(moving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)));
        if (Input.GetKeyDown(KeyCode.M)) PlayGesture(); else if (Input.GetKeyDown(KeyCode.J)) PlayJump(); else if (Input.GetKeyDown(KeyCode.Space)) PlayRoll(); else if (Input.GetKeyDown(KeyCode.T)) PlayThrow();
#endif
    }
    public void SetRunning(bool running)
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator && animator.runtimeAnimatorController) animator.SetBool(RunningParameter, running);
    }
    public void SetWalking(bool walking)
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator && animator.runtimeAnimatorController) animator.SetBool(WalkingParameter, walking);
    }
    bool BusyWith(string state)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(state) || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(state));
    }
    public void PlayJump()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!animator || !animator.runtimeAnimatorController || BusyWith("Jump") || BusyWith("Roll") || BusyWith("Throw") || BusyWith("MiddleFinger")) return;
        animator.ResetTrigger(ThrowParameter); animator.ResetTrigger(RollParameter); animator.SetTrigger(JumpParameter);
    }
    public void PlayRoll()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!animator || !animator.runtimeAnimatorController || BusyWith("Jump") || BusyWith("MiddleFinger")) return;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Roll") || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Roll"))) return;
        animator.ResetTrigger(ThrowParameter); animator.ResetTrigger(JumpParameter); animator.SetTrigger(RollParameter);
    }
    public void PlayThrow()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (animator && animator.runtimeAnimatorController && !BusyWith("MiddleFinger") && !BusyWith("Jump") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Roll") && !(animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Roll"))) animator.SetTrigger(ThrowParameter);
    }
    public void PlayGesture()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!animator || !animator.runtimeAnimatorController || BusyWith("MiddleFinger") || BusyWith("Jump") || BusyWith("Roll") || BusyWith("Throw")) return;
        animator.ResetTrigger(ThrowParameter); animator.ResetTrigger(JumpParameter); animator.ResetTrigger(RollParameter); animator.SetTrigger(GestureParameter);
    }

    SkinnedMeshRenderer gestureHandRenderer;
    Mesh originalHandMesh, gestureHandMesh;
    public void SetGestureHandPreview(bool show)
    {
        if (!gestureHandRenderer)
        {
            foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin.name == "tripo_part_2") { gestureHandRenderer=skin; originalHandMesh=skin.sharedMesh; break; }
        }
        if (!gestureHandRenderer) return;
        if (show && !gestureHandMesh) gestureHandMesh=Resources.Load<Mesh>("PotionGestureRightHand");
        gestureHandRenderer.sharedMesh=show && gestureHandMesh ? gestureHandMesh : originalHandMesh;
    }
    public void RefreshGestureHand()
    {
        if (!animator) animator=GetComponent<Animator>();
        if (!animator || !animator.runtimeAnimatorController) { SetGestureHandPreview(false); return; }
        var state=animator.GetCurrentAnimatorStateInfo(0);
        SetGestureHandPreview(state.IsName("MiddleFinger") && state.normalizedTime>=.065f && state.normalizedTime<.93f);
    }
    void LateUpdate() { RefreshGestureHand(); }
    void OnDisable() { if(gestureHandRenderer && originalHandMesh) gestureHandRenderer.sharedMesh=originalHandMesh; }

}
