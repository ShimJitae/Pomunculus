using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// PhotonTransformView 동작을 따라한 Transform 동기화. PhotonView의 ViewID 대신 IWorldObj.ID로 대상을 찾는다
// 송신자는 일정 주기로 위치/이동량/회전/스케일을 보내고, 수신자는 다음 패킷이 올 때까지 그 값으로 보간 이동
public class WorldTransformSync : MonoBehaviour
{
    #region Unity
    private void Awake()
    {
        m_owner = GetComponent<IWorldObj>();
        if (m_owner == null) Debug.LogError("[WorldTransformSync] 같은 오브젝트에 IWorldObj가 없음", this);
        m_storedPosition = GetPosition();
        m_lastRotation = GetRotation();
        m_lastScale = transform.localScale;
        m_networkPosition = Vector3.zero;
        m_networkRotation = Quaternion.identity;
    }
    private void Reset()
    {
        m_useLocal = true;
    }
    private void OnEnable()
    {
        m_firstTake = true;
    }
    // ID는 WorldManager가 Instantiate 직후 InitWorldObj로 넣어주므로 Awake/OnEnable이 아니라 Start에서 등록
    private void Start()
    {
        if (m_owner == null) return;
        m_registeredId = m_owner.ID;
        s_syncs[m_registeredId] = this;
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.TransformSync, OnReceive); // 중복 등록돼도 덮어씀
    }
    private void OnDestroy()
    {
        if (s_syncs.TryGetValue(m_registeredId, out var sync) && sync == this) s_syncs.Remove(m_registeredId);
    }
    private void Update()
    {
        if (m_owner == null) return;
        if (IsOwner())
        {
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1) TrySend();
            return;
        }
        // 다음 패킷이 올 시점(1 / sendRate초)에 목표에 도착하도록 이동
        if (m_synchronizePosition)
            SetPosition(Vector3.MoveTowards(GetPosition(), m_networkPosition, m_distance * Time.deltaTime * m_sendRate));
        if (m_synchronizeRotation)
            SetRotation(Quaternion.RotateTowards(GetRotation(), m_networkRotation, m_angle * Time.deltaTime * m_sendRate));
    }
    #endregion

    // 기본은 방장이 송신자. 플레이어처럼 각자 조종하는 오브젝트는 외부에서 교체
    public Func<bool> IsOwner { get; set; } = () => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    #region 송신
    private void TrySend()
    {
        m_sendTimer += Time.deltaTime;
        m_keyframeTimer += Time.deltaTime;
        m_elapsedSinceSend += Time.deltaTime;
        if (m_sendTimer < 1f / m_sendRate) return;
        m_sendTimer = 0;

        var pos = GetPosition();
        var rot = GetRotation();
        var scale = transform.localScale;
        // 마지막으로 보낸 값과 비교. 조금씩 움직여도 누적되면 임계값을 넘어서 전송됨
        bool changed =
            (m_synchronizePosition && (pos - m_storedPosition).sqrMagnitude > m_positionThreshold * m_positionThreshold) ||
            (m_synchronizeRotation && Quaternion.Angle(rot, m_lastRotation) > m_rotationThreshold) ||
            (m_synchronizeScale && (scale - m_lastScale).sqrMagnitude > m_scaleThreshold * m_scaleThreshold);
        bool isFinal = !changed && m_wasChanged; // 멈춘 직후, 속도 0과 최종 위치를 reliable로 한 번 확정
        bool isKeyframe = m_keyframeInterval > 0 && m_keyframeTimer >= m_keyframeInterval; // 유실 대비
        m_wasChanged = changed;
        if (!changed && !isFinal && !isKeyframe) return;
        m_keyframeTimer = 0;

        PacketData d = new();
        d.Write((int)WorldEventID.TransformSync);
        d.Write(m_owner.ID);
        d.Write(PhotonNetwork.ServerTimestamp); // 수신 측 지연 보정용
        if (m_synchronizePosition)
        {
            // PhotonTransformView는 전송 간 이동량을 그대로 보내지만, 지연(초)과 곱하기 위해 초당 속도로 보냄
            // 멈춰 있으면 속도 0을 보내서 수신 측이 예측 이동하지 않게 함
            var velocity = changed ? (pos - m_storedPosition) / m_elapsedSinceSend : Vector3.zero;
            m_storedPosition = pos;
            d.Write(pos);
            d.Write(velocity);
        }
        if (m_synchronizeRotation) WriteQuaternion(d, rot);
        if (m_synchronizeScale) d.Write(scale);
        m_lastRotation = rot;
        m_lastScale = scale;
        m_elapsedSinceSend = 0;

        // 움직이는 중에는 다음 패킷이 덮으니 unreliable, 멈출 때의 확정 패킷만 reliable
        RaiseEventManager.Instance.SendPacket(d, new() { Receivers = ReceiverGroup.Others },
            isFinal ? SendOptions.SendReliable : SendOptions.SendUnreliable);
    }
    #endregion

    #region 수신
    private static void OnReceive(EventData e, PacketData p)
    {
        int id = p;
        if (s_syncs.TryGetValue(id, out var sync)) sync.Deserialize(p);
    }
    private void Deserialize(PacketData d)
    {
        if (IsOwner()) return;

        int sentTime = d;
        if (m_synchronizePosition)
        {
            m_networkPosition = d.ReadVector3();
            var velocity = d.ReadVector3();
            if (m_firstTake)
            {
                SetPosition(m_networkPosition);
                m_distance = 0f;
            }
            else
            {
                // ServerTimestamp는 int ms이고 오버플로 되므로 뺄셈 결과만 사용
                float lag = Mathf.Abs(unchecked(PhotonNetwork.ServerTimestamp - sentTime) / 1000f);
                m_networkPosition += velocity * lag;
                m_distance = Vector3.Distance(GetPosition(), m_networkPosition);
            }
        }
        if (m_synchronizeRotation)
        {
            m_networkRotation = ReadQuaternion(d);
            if (m_firstTake)
            {
                SetRotation(m_networkRotation);
                m_angle = 0f;
            }
            else
            {
                m_angle = Quaternion.Angle(GetRotation(), m_networkRotation);
            }
        }
        if (m_synchronizeScale) transform.localScale = d.ReadVector3();

        m_firstTake = false;
    }
    #endregion

    #region 유틸
    private Vector3 GetPosition() => m_useLocal ? transform.localPosition : transform.position;
    private Quaternion GetRotation() => m_useLocal ? transform.localRotation : transform.rotation;
    private void SetPosition(Vector3 v)
    {
        if (m_useLocal) transform.localPosition = v;
        else transform.position = v;
    }
    private void SetRotation(Quaternion q)
    {
        if (m_useLocal) transform.localRotation = q;
        else transform.rotation = q;
    }
    private static void WriteQuaternion(PacketData d, Quaternion q)
    {
        d.Write(q.x);
        d.Write(q.y);
        d.Write(q.z);
        d.Write(q.w);
    }
    private static Quaternion ReadQuaternion(PacketData d)
    {
        float x = d, y = d, z = d, w = d;
        return new Quaternion(x, y, z, w);
    }
    #endregion

    private static readonly Dictionary<int, WorldTransformSync> s_syncs = new();

    [SerializeField] private bool m_synchronizePosition = true;
    [SerializeField] private bool m_synchronizeRotation = true;
    [SerializeField] private bool m_synchronizeScale = false;
    [Tooltip("localPosition/localRotation 사용 여부. 스케일은 항상 localScale")]
    [SerializeField] private bool m_useLocal;
    [SerializeField, Min(1)] private float m_sendRate = 10f; // PhotonNetwork.SerializationRate 기본값과 동일
    [Header("변화 감지")]
    [SerializeField, Min(0)] private float m_positionThreshold = 0.01f; // 이 거리 이하로 움직이면 변화 없음
    [SerializeField, Min(0)] private float m_rotationThreshold = 0.5f; // 각도(도)
    [SerializeField, Min(0)] private float m_scaleThreshold = 0.01f;
    [SerializeField, Min(0)] private float m_keyframeInterval = 1f; // 변화가 없어도 이 간격마다 전송. 0이면 끔

    private IWorldObj m_owner;
    private int m_registeredId = -1;

    private float m_distance;
    private float m_angle;
    private Vector3 m_networkPosition;
    private Vector3 m_storedPosition;
    private Quaternion m_networkRotation;
    private bool m_firstTake;
    private float m_sendTimer;
    private float m_keyframeTimer;
    private float m_elapsedSinceSend; // 마지막 전송 후 지난 시간. 속도 계산용
    private Quaternion m_lastRotation; // 마지막으로 보낸 값 (위치는 m_storedPosition)
    private Vector3 m_lastScale;
    private bool m_wasChanged;
}
