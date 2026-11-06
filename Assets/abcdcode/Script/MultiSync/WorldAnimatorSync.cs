using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// PhotonAnimatorView 동작을 따라한 Animator 동기화. PhotonView의 ViewID 대신 IWorldObj.ID로 대상을 찾는다
// Continuous : 송신자가 매 샘플마다 값을 기록해 묶어 보내고, 수신자가 같은 간격으로 재생 (부드러움, 데이터 많음)
// Discrete : 송신 시점의 값만 보냄 (데이터 적음, 트리거는 이 방식 권장)
// State : 파라미터가 아니라 Animator.Play/CrossFade로 직접 재생한 상태도 동기화. 소유자 쪽 Animator의 현재/다음 상태를 관찰해서 보냄
// 같은 프리팹끼리만 주고받는다고 가정하므로 레이어/파라미터 목록 순서가 송수신 양쪽에서 같아야 함
[RequireComponent(typeof(Animator))]
public class WorldAnimatorSync : MonoBehaviour
{
    public enum ParameterType
    {
        Float = 1,
        Int = 3,
        Bool = 4,
        Trigger = 9, // AnimatorControllerParameterType과 같은 값
    }
    public enum SynchronizeType
    {
        Disabled = 0,
        Discrete = 1,
        Continuous = 2,
    }
    [Serializable]
    public class SynchronizedParameter
    {
        public ParameterType Type;
        public SynchronizeType SynchronizeType;
        public string Name;
        [NonSerialized] public int Hash;
    }
    [Serializable]
    public class SynchronizedLayer
    {
        public SynchronizeType SynchronizeType;
        public int LayerIndex;
    }

    #region Unity
    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_owner = GetComponent<IWorldObj>();
        if (m_owner == null) Debug.LogError("[WorldAnimatorSync] 같은 오브젝트에 IWorldObj가 없음", this);
        foreach (var p in m_parameters) p.Hash = Animator.StringToHash(p.Name);
        BuildSyncList();
        m_stateTracks = new StateTrack[m_stateLayers.Count];
    }
    // ID는 WorldManager가 Instantiate 직후 InitWorldObj로 넣어주므로 Awake/OnEnable이 아니라 Start에서 등록
    private void Start()
    {
        if (m_owner == null) return;
        m_registeredId = m_owner.ID;
        s_syncs[m_registeredId] = this;
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.AnimatorSync, OnReceive); // 중복 등록돼도 덮어씀
        RaiseEventManager.Instance.RegisterEvent((int)WorldEventID.AnimatorState, OnReceiveState);
    }
    private void OnDestroy()
    {
        if (s_syncs.TryGetValue(m_registeredId, out var sync) && sync == this) s_syncs.Remove(m_registeredId);
    }
    private void Update()
    {
        if (m_owner == null) return;
        bool isOwner = IsOwner();
        // 원격 오브젝트는 루트모션으로 위치가 따로 움직이지 않도록
        if (!isOwner && m_animator.applyRootMotion) m_animator.applyRootMotion = false;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            m_samples.Clear();
            m_receivedSamples.Clear();
            m_raisedTriggers.Clear();
            return;
        }
        if (isOwner)
        {
            RecordSamples();
            CacheDiscreteTriggers();
            TrySend();
        }
        else
        {
            PlayReceivedSamples();
        }
    }
    // Update에서 호출한 Play/CrossFade는 Animator 갱신(Update와 LateUpdate 사이) 뒤에 상태 정보에 반영되므로 LateUpdate에서 관찰
    private void LateUpdate()
    {
        if (m_owner == null || m_stateTracks.Length == 0 || !IsOwner()) return;
        TrySendState();
    }
    #endregion

    // 기본은 방장이 송신자. 플레이어처럼 각자 조종하는 오브젝트는 외부에서 교체
    public Func<bool> IsOwner { get; set; } = () => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    // Animator.SetTrigger 대신 이걸 쓰면 트리거가 확실히 전송됨
    public void SetTrigger(string name)
    {
        m_animator.SetTrigger(name);
        m_raisedTriggers.Add(Animator.StringToHash(name));
    }

    #region 송신
    private void RecordSamples()
    {
        if (m_continuousLayers.Count == 0 && m_continuousParams.Count == 0) return;
        m_sampleTimer += Time.deltaTime;
        float interval = 1f / m_sampleRate;
        if (m_sampleTimer < interval) return;
        m_sampleTimer %= interval;

        m_samples.Add(CaptureContinuous());
        if (m_samples.Count > MaxSamplesPerSend) m_samples.RemoveAt(0);
    }
    private float[] CaptureContinuous()
    {
        var sample = new float[m_continuousLayers.Count + m_continuousParams.Count];
        int i = 0;
        foreach (var l in m_continuousLayers) sample[i++] = m_animator.GetLayerWeight(l.LayerIndex);
        foreach (var p in m_continuousParams) sample[i++] = GetValue(p);
        return sample;
    }
    // 트리거는 m_raisedTriggers로 따로 판단하므로 항상 0
    private float[] CaptureDiscrete()
    {
        var values = new float[m_discreteLayers.Count + m_discreteParams.Count];
        int i = 0;
        foreach (var l in m_discreteLayers) values[i++] = m_animator.GetLayerWeight(l.LayerIndex);
        foreach (var p in m_discreteParams) values[i++] = p.Type == ParameterType.Trigger ? 0f : GetValue(p);
        return values;
    }
    // PhotonAnimatorView와 같은 방식. 트랜지션이 트리거를 먼저 소비하면 놓칠 수 있으므로 SetTrigger 사용 권장
    private void CacheDiscreteTriggers()
    {
        foreach (var p in m_discreteParams)
            if (p.Type == ParameterType.Trigger && m_animator.GetBool(p.Hash)) m_raisedTriggers.Add(p.Hash);
    }
    private void TrySend()
    {
        m_sendTimer += Time.deltaTime;
        m_keyframeTimer += Time.deltaTime;
        if (m_sendTimer < 1f / m_sendRate) return;
        m_sendTimer = 0;

        var discrete = CaptureDiscrete();
        bool hasTrigger = m_raisedTriggers.Count > 0;
        bool changed = hasTrigger || IsChanged(discrete, m_lastDiscrete) || m_samples.Exists(s => IsChanged(s, m_lastSample));
        bool isFinal = !changed && m_wasChanged; // 변화가 멈춘 직후, 마지막 상태를 reliable로 한 번 확정
        bool isKeyframe = m_keyframeInterval > 0 && m_keyframeTimer >= m_keyframeInterval; // 늦게 들어온 플레이어/유실 대비
        m_wasChanged = changed;
        if (!changed && !isFinal && !isKeyframe)
        {
            m_samples.Clear();
            return;
        }
        m_keyframeTimer = 0;
        // 변화가 없으면 쌓인 샘플 대신 현재 값 하나만 보냄
        if (!changed || m_samples.Count == 0)
        {
            m_samples.Clear();
            m_samples.Add(CaptureContinuous());
        }

        PacketData d = new();
        d.Write((int)WorldEventID.AnimatorSync);
        d.Write(m_owner.ID);

        d.Write(m_samples.Count);
        foreach (var sample in m_samples)
            foreach (var v in sample) d.Write(v);

        int i = m_discreteLayers.Count;
        for (int l = 0; l < m_discreteLayers.Count; l++) d.Write(discrete[l]);
        foreach (var p in m_discreteParams)
        {
            float v = discrete[i++];
            switch (p.Type)
            {
                case ParameterType.Bool: d.Write(v > 0.5f); break;
                case ParameterType.Float: d.Write(v); break;
                case ParameterType.Int: d.Write(Mathf.RoundToInt(v)); break;
                case ParameterType.Trigger: d.Write(m_raisedTriggers.Contains(p.Hash)); break;
            }
        }

        m_lastSample = m_samples[^1];
        m_lastDiscrete = discrete;
        m_samples.Clear();
        m_raisedTriggers.Clear();
        // 트리거/확정 패킷은 유실되면 상태가 어긋나므로 reliable, 나머지는 다음 패킷이 덮으니 unreliable
        RaiseEventManager.Instance.SendPacket(d, new() { Receivers = ReceiverGroup.Others },
            hasTrigger || isFinal ? SendOptions.SendReliable : SendOptions.SendUnreliable);
    }
    // 레이어별로 재생 중(트랜지션 중이면 다음) 상태가 바뀌었거나 같은 상태를 처음부터 다시 재생했으면 reliable로 전송
    // 변화가 없어도 m_keyframeInterval마다 현재 상태를 unreliable로 보내서 늦게 들어온 플레이어/어긋난 상태를 맞춤
    private void TrySendState()
    {
        m_changedStates.Clear();
        for (int i = 0; i < m_stateTracks.Length; i++)
        {
            int layer = m_stateLayers[i];
            if (layer < 0 || layer >= m_animator.layerCount) continue;
            bool inTransition = m_animator.IsInTransition(layer);
            var info = inTransition ? m_animator.GetNextAnimatorStateInfo(layer) : m_animator.GetCurrentAnimatorStateInfo(layer);
            ref var track = ref m_stateTracks[i];
            bool changed = info.fullPathHash != track.Hash;
            // normalizedTime은 루프해도 계속 증가하므로, 같은 상태에서 줄어들었다면 Play(hash, layer, 0)로 다시 재생한 것
            bool restarted = !changed && info.normalizedTime + RestartThreshold < track.NormalizedTime;
            track.Hash = info.fullPathHash;
            track.NormalizedTime = info.normalizedTime;
            if (!changed && !restarted) continue;

            var entry = new StateEntry { Layer = layer, Hash = info.fullPathHash, NormalizedTime = info.normalizedTime };
            if (restarted) entry.Mode = StateMode.Restart;
            else if (inTransition)
            {
                entry.Mode = StateMode.CrossFade;
                var transition = m_animator.GetAnimatorTransitionInfo(layer);
                // 수신 측은 CrossFadeInFixedTime으로 재생하므로 초 단위로 맞춤
                entry.Duration = transition.durationUnit == DurationUnit.Fixed
                    ? transition.duration
                    : transition.duration * m_animator.GetCurrentAnimatorStateInfo(layer).length;
            }
            else entry.Mode = StateMode.Play;
            m_changedStates.Add(entry);
        }

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            m_stateKeyframeTimer = 0;
            return;
        }
        m_stateKeyframeTimer += Time.deltaTime;
        if (m_changedStates.Count > 0)
        {
            SendState(m_changedStates, false);
            m_stateKeyframeTimer = 0;
        }
        else if (m_keyframeInterval > 0 && m_stateKeyframeTimer >= m_keyframeInterval)
        {
            m_stateKeyframeTimer = 0;
            for (int i = 0; i < m_stateTracks.Length; i++)
                m_changedStates.Add(new StateEntry { Layer = m_stateLayers[i], Hash = m_stateTracks[i].Hash, NormalizedTime = m_stateTracks[i].NormalizedTime, Mode = StateMode.Play });
            SendState(m_changedStates, true);
        }
    }
    private void SendState(List<StateEntry> entries, bool isKeyframe)
    {
        PacketData d = new();
        d.Write((int)WorldEventID.AnimatorState);
        d.Write(m_owner.ID);
        d.Write(isKeyframe);
        d.Write(entries.Count);
        foreach (var e in entries)
        {
            d.Write(e.Layer);
            d.Write(e.Hash);
            d.Write((byte)e.Mode);
            d.Write(e.Duration);
            d.Write(e.NormalizedTime);
        }
        RaiseEventManager.Instance.SendPacket(d, new() { Receivers = ReceiverGroup.Others },
            isKeyframe ? SendOptions.SendUnreliable : SendOptions.SendReliable);
    }
    private bool IsChanged(float[] a, float[] b)
    {
        if (b == null || a.Length != b.Length) return true;
        for (int i = 0; i < a.Length; i++)
            if (Mathf.Abs(a[i] - b[i]) > m_changeThreshold) return true;
        return false;
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

        int count = d;
        int length = m_continuousLayers.Count + m_continuousParams.Count;
        for (int s = 0; s < count; s++)
        {
            var sample = new float[length];
            for (int i = 0; i < length; i++) sample[i] = d;
            m_receivedSamples.Enqueue(sample);
        }
        // 밀린 샘플이 너무 많으면 최신 쪽으로 따라잡음
        while (m_receivedSamples.Count > MaxSamplesPerSend * 2) m_receivedSamples.Dequeue();

        foreach (var l in m_discreteLayers) m_animator.SetLayerWeight(l.LayerIndex, d);
        foreach (var p in m_discreteParams)
        {
            switch (p.Type)
            {
                case ParameterType.Bool: m_animator.SetBool(p.Hash, d); break;
                case ParameterType.Float: m_animator.SetFloat(p.Hash, d); break;
                case ParameterType.Int: m_animator.SetInteger(p.Hash, d); break;
                case ParameterType.Trigger:
                    bool raised = d;
                    if (raised) m_animator.SetTrigger(p.Hash);
                    break;
            }
        }
    }
    private static void OnReceiveState(EventData e, PacketData p)
    {
        int id = p;
        if (s_syncs.TryGetValue(id, out var sync)) sync.DeserializeState(p);
    }
    private void DeserializeState(PacketData d)
    {
        if (IsOwner()) return;
        bool isKeyframe = d;
        int count = d;
        for (int i = 0; i < count; i++)
        {
            int layer = d;
            int hash = d;
            var mode = (StateMode)d.ReadByte();
            float duration = d;
            float normalizedTime = d;
            if (layer < 0 || layer >= m_animator.layerCount || hash == 0) continue;
            // 이미 그 상태이거나 그 상태로 가는 중이면 무시. 종료 시간 트랜지션처럼 원격에서도 알아서 넘어간 경우
            if (mode != StateMode.Restart && IsPlayingOrEntering(layer, hash)) continue;
            switch (mode)
            {
                case StateMode.CrossFade: m_animator.CrossFadeInFixedTime(hash, duration, layer); break;
                case StateMode.Restart: m_animator.Play(hash, layer, 0f); break;
                // 키프레임은 송신 시점 진행도로 맞추고, 상태가 바뀐 직후의 Play는 처음부터
                default: m_animator.Play(hash, layer, isKeyframe ? normalizedTime : 0f); break;
            }
        }
    }
    private bool IsPlayingOrEntering(int layer, int hash)
    {
        return m_animator.IsInTransition(layer)
            ? m_animator.GetNextAnimatorStateInfo(layer).fullPathHash == hash
            : m_animator.GetCurrentAnimatorStateInfo(layer).fullPathHash == hash;
    }
    private void PlayReceivedSamples()
    {
        if (m_receivedSamples.Count == 0) return;
        m_playTimer += Time.deltaTime;
        float interval = 1f / m_sampleRate;
        if (m_playTimer < interval) return;
        m_playTimer %= interval;

        var sample = m_receivedSamples.Dequeue();
        int i = 0;
        foreach (var l in m_continuousLayers) m_animator.SetLayerWeight(l.LayerIndex, sample[i++]);
        foreach (var p in m_continuousParams) SetValue(p, sample[i++]);
    }
    #endregion

    #region 유틸
    private void BuildSyncList()
    {
        m_continuousLayers.Clear();
        m_discreteLayers.Clear();
        m_continuousParams.Clear();
        m_discreteParams.Clear();
        foreach (var l in m_layers)
        {
            if (l.SynchronizeType == SynchronizeType.Continuous) m_continuousLayers.Add(l);
            else if (l.SynchronizeType == SynchronizeType.Discrete) m_discreteLayers.Add(l);
        }
        foreach (var p in m_parameters)
        {
            if (p.SynchronizeType == SynchronizeType.Continuous) m_continuousParams.Add(p);
            else if (p.SynchronizeType == SynchronizeType.Discrete) m_discreteParams.Add(p);
        }
    }
    // Continuous 값은 전부 float로 주고받음
    private float GetValue(SynchronizedParameter p) => p.Type switch
    {
        ParameterType.Float => m_animator.GetFloat(p.Hash),
        ParameterType.Int => m_animator.GetInteger(p.Hash),
        _ => m_animator.GetBool(p.Hash) ? 1f : 0f,
    };
    private void SetValue(SynchronizedParameter p, float v)
    {
        switch (p.Type)
        {
            case ParameterType.Float: m_animator.SetFloat(p.Hash, v); break;
            case ParameterType.Int: m_animator.SetInteger(p.Hash, Mathf.RoundToInt(v)); break;
            default: m_animator.SetBool(p.Hash, v > 0.5f); break; // Trigger도 PhotonAnimatorView처럼 bool로 처리
        }
    }
    private int MaxSamplesPerSend => Mathf.Max(1, Mathf.CeilToInt(m_sampleRate / m_sendRate));

#if UNITY_EDITOR
    // PhotonAnimatorView 인스펙터의 자동 목록 생성 대신 사용. 기존 설정은 유지
    [ContextMenu("Animator에서 레이어/파라미터 가져오기")]
    private void FillFromAnimator()
    {
        var controller = GetComponent<Animator>().runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        if (controller == null) return;
        for (int i = 0; i < controller.layers.Length; i++)
            if (!m_layers.Exists(x => x.LayerIndex == i))
                m_layers.Add(new SynchronizedLayer { LayerIndex = i, SynchronizeType = SynchronizeType.Disabled });
        foreach (var p in controller.parameters)
            if (!m_parameters.Exists(x => x.Name == p.name))
                m_parameters.Add(new SynchronizedParameter { Name = p.name, Type = (ParameterType)p.type, SynchronizeType = SynchronizeType.Disabled });
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
    #endregion

    private static readonly Dictionary<int, WorldAnimatorSync> s_syncs = new();

    [SerializeField] private List<SynchronizedLayer> m_layers = new();
    [SerializeField] private List<SynchronizedParameter> m_parameters = new();
    [SerializeField, Min(1)] private float m_sendRate = 10f; // PhotonNetwork.SerializationRate 기본값과 동일
    [SerializeField, Min(1)] private float m_sampleRate = 30f; // Continuous 기록/재생 간격
    [SerializeField, Min(0)] private float m_changeThreshold = 0.001f; // 이 값 이하로 바뀌면 변화 없음으로 봄
    [SerializeField, Min(0)] private float m_keyframeInterval = 1f; // 변화가 없어도 이 간격마다 전송. 0이면 끔
    [SerializeField] private List<int> m_stateLayers = new() { 0 }; // 재생 상태를 동기화할 레이어. 비우면 상태 동기화 안 함

    private Animator m_animator;
    private IWorldObj m_owner;
    private int m_registeredId = -1;

    private readonly List<SynchronizedLayer> m_continuousLayers = new();
    private readonly List<SynchronizedLayer> m_discreteLayers = new();
    private readonly List<SynchronizedParameter> m_continuousParams = new();
    private readonly List<SynchronizedParameter> m_discreteParams = new();

    private readonly List<float[]> m_samples = new();
    private readonly Queue<float[]> m_receivedSamples = new();
    private readonly HashSet<int> m_raisedTriggers = new();
    private float m_sampleTimer;
    private float m_sendTimer;
    private float m_playTimer;
    private float m_keyframeTimer;
    private float[] m_lastSample; // 마지막으로 보낸 Continuous 값
    private float[] m_lastDiscrete; // 마지막으로 보낸 Discrete 값
    private bool m_wasChanged;

    private enum StateMode : byte
    {
        Play,       // 트랜지션 없이 바로 전환
        CrossFade,  // 트랜지션으로 전환 (Duration초)
        Restart,    // 같은 상태를 처음부터 다시 재생
    }
    private struct StateTrack
    {
        public int Hash;
        public float NormalizedTime;
    }
    private struct StateEntry
    {
        public int Layer;
        public int Hash;
        public StateMode Mode;
        public float Duration;
        public float NormalizedTime;
    }
    private const float RestartThreshold = 0.01f;
    private StateTrack[] m_stateTracks;
    private readonly List<StateEntry> m_changedStates = new();
    private float m_stateKeyframeTimer;
}
