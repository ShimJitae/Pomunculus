using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smooth = 5f;
    private Vector3 offset;

    void Start() => offset = transform.position - target.position;  // 처음 배치한 거리 유지

    void LateUpdate()  // 플레이어 이동 후에 따라가도록 LateUpdate
    {
        transform.position = Vector3.Lerp(
            transform.position, target.position + offset, smooth * Time.deltaTime);
    }
}
