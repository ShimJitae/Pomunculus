using UnityEngine;

public sealed class WorldMapUI : MonoBehaviour
{
    [SerializeField] private Transform worldRoot;
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform playerPin;
    [SerializeField] private WorldMapCalibration calibration;

    public void Open()
    {
        gameObject.SetActive(true);

        if (playerPin == null)
        {
            return;
        }

        playerPin.gameObject.SetActive(false);

        if (target == null)
        {
            target = FindLocalPlayer();
        }

        if (target == null || !target.gameObject.activeInHierarchy ||
            worldRoot == null || calibration == null)
        {
            return;
        }

        Vector3 localPosition = worldRoot.InverseTransformPoint(target.position);
        Vector2 worldXZ = new Vector2(localPosition.x, localPosition.z);

        if (!calibration.TryGetMapUV(worldXZ, out Vector2 imageUV))
        {
            return;
        }

        playerPin.anchorMin = imageUV;
        playerPin.anchorMax = imageUV;
        playerPin.anchoredPosition = Vector2.zero;
        playerPin.gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private static Transform FindLocalPlayer()
    {
        foreach (IWorldObj worldObject in WorldManager.Instance.Objects.GetList())
        {
            if (worldObject is PlayerFSM player &&
                player != null &&
                player.IsMine &&
                player.gameObject.activeInHierarchy)
            {
                return player.transform;
            }
        }

        return null;
    }
}