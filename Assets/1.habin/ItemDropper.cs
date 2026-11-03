using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    [SerializeField] private GameObject itemPrefab;

    public void DropItems()
    {
        if (itemPrefab == null)
            return;

        Instantiate(itemPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
    }
}