using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(SphereCollider))]
public class EnemyDetector : MonoBehaviour
{
    public event Action OnPlayersChanged;

    private SphereCollider detectCollider;
    private readonly List<GameObject> playersInRange = new();

    private void Awake()
    {
        detectCollider = GetComponent<SphereCollider>();
        detectCollider.isTrigger = true;
    }

    private void OnDisable()
    {
        playersInRange.Clear();
    }

    public void SetRadius(float radius)
    {
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
        if (playersInRange.Remove(other.gameObject) || playersInRange.Contains(other.gameObject))
            return;

        OnPlayersChanged?.Invoke();
    }

    public bool Contains(GameObject player)
    {
        return playersInRange.Contains(player);
    }

    public GameObject GetNearestPlayer()
    {
        GameObject nearset = null;
        float minSqr = float.MaxValue;

        for (int i = playersInRange.Count - 1; i >= 0; i--)
        {
            GameObject obj = playersInRange[i];

            if(obj == null || !obj.activeInHierarchy)
            {
                playersInRange.RemoveAt(i);
                continue;
            }

            float sqr = (obj.transform.position - transform.position).sqrMagnitude;

            if(sqr <  minSqr)
            {
                minSqr = sqr;
                nearset = obj;
            }
        }
        return nearset;
    }
}
