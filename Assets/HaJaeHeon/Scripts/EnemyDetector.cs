using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(SphereCollider)), RequireComponent(typeof(Rigidbody))]
public class EnemyDetector : MonoBehaviour
{
    public event Action OnPlayersChanged;

    private readonly List<GameObject> playersInRange = new();
    
    [SerializeField] private SphereCollider detectCollider;
    [SerializeField] private Rigidbody detectRigidbody;


    private void OnEnable()
    {
        detectCollider = GetComponent<SphereCollider>();
        detectCollider.isTrigger = true;

        detectRigidbody = GetComponent<Rigidbody>();
        detectRigidbody.isKinematic = true;
        detectRigidbody.useGravity = false;
    }

    private void OnDisable()
    {
        playersInRange.Clear();
    }

    public void SetRadius(float radius)
    {
        if(detectCollider == null)
            detectCollider = GetComponent<SphereCollider>();

        detectCollider.radius = radius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || playersInRange.Contains(other.gameObject))
            return;

        playersInRange.Add(other.gameObject);

        OnPlayersChanged?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playersInRange.Remove(other.gameObject))
            return;

        OnPlayersChanged?.Invoke();
    }

    public bool Contains(GameObject player)
    {
        return playersInRange.Contains(player);
    }

    // 제거되거나 비활성화된 후보를 정리하면서 가장 가까운 플레이어를 찾는다.
    public GameObject GetNearestPlayer()
    {
        GameObject nearestPlayer = null;
        float minSqrDistance = float.MaxValue;

        // 탐색 중 후보를 삭제해도 아직 검사하지 않은 항목을 건너뛰지 않도록 역순으로 순회
        for (int i = playersInRange.Count - 1; i >= 0; i--)
        {
            GameObject player = playersInRange[i];

            if(player == null || !player.activeInHierarchy)
            {
                playersInRange.RemoveAt(i);
                continue;
            }

            float sqrDistance = (player.transform.position - transform.position).sqrMagnitude;

            if(sqrDistance <  minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearestPlayer = player;
            }
        }
        return nearestPlayer;
    }
}
