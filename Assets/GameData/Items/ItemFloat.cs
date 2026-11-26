using UnityEngine;

public class ItemFloat : MonoBehaviour
{
    [Header("위아래 이동 높이")]
    [SerializeField, Min(0f)] private float height = 0.1f;
    [Header("움직임 속도")]
    [SerializeField, Min(0f)] private float speed = 2f;
    private Vector3 startPosition;
    private float elapsedTime;

    private void Awake()
    {
        startPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        float offset = (1f - Mathf.Cos(elapsedTime * speed)) * 0.5f * height;
        Vector3 localUp = transform.parent != null
            ? transform.parent.InverseTransformVector(Vector3.up)
            : Vector3.up;

        transform.localPosition = startPosition + localUp * offset;
    }

    private void OnDisable()
    {
        transform.localPosition = startPosition;
    }
}