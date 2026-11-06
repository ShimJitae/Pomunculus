using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(SphereCollider)), RequireComponent(typeof(Rigidbody))]
public class EnemyDetector : MonoBehaviour
{
    public event Action OnPlayersChanged;

    private readonly List<GameObject> playersInRange = new();
    
    [SerializeField] private SphereCollider detectCollider;
    [SerializeField] private Rigidbody rb;


    private void OnEnable()
    {
        detectCollider = GetComponent<SphereCollider>();
        detectCollider.isTrigger = true;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
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
