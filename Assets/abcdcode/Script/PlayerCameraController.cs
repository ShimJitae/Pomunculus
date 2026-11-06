
using Unity.Cinemachine;
using UnityEngine;

public class PlayerCameraController : SingletonBehavior<PlayerCameraController>
{
    public void SetPlayer(IWorldObj target)
    {
        m_camera.Target.TrackingTarget = target.gameObject.transform;
    }
    [SerializeField] private CinemachineCamera m_camera;
}