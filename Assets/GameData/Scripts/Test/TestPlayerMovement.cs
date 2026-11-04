using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Pomunculus.Items
{
    [RequireComponent(typeof(CharacterController))]
    public class TestPlayerMovement : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("점프")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 input = GetMoveInput();

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (controller.isGrounded && GetJumpPressed())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 movement = new Vector3(input.x * moveSpeed, verticalVelocity, input.y * moveSpeed);
            controller.Move(movement * Time.deltaTime);
        }

        private Vector2 GetMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return Vector2.zero;

            Vector2 direction = Vector2.zero;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                direction.x -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                direction.x += 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                direction.y += 1f;

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                direction.y -= 1f;

            return direction.normalized;
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
        }

        private bool GetJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}