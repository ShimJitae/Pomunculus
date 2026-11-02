using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private Camera mainCamera;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] LayerMask groundLayer;

    Vector3 targetPos;
    bool isMoving;


    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse.rightButton.isPressed)
        {
            Debug.Log("마우스 클릭데스");
            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                targetPos = hit.point;
                targetPos.y = transform.position.y;
                isMoving = true;
            }
        }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, targetPos, moveSpeed * Time.deltaTime);

            Vector3 dir = targetPos - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);  // 진행 방향 바라보기
            else
                isMoving = false;
        }



    }
}

