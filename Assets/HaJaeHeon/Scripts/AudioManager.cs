using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum AudioChannelType
{
    BGM,
    SFX,
    UI,
    MASTER,
}
public class AudioManager : SingletonBehavior<AudioManager>
{
    public readonly struct SoundHandle
    {
        public readonly int Index;
        public readonly int PlayId;
        public bool IsValid => PlayId != 0;

        public SoundHandle(int index, int playId)
        {
            Index = index;
            PlayId = playId;
        }
    }
    
    private const string MasterVolumeParam = "MasterVolume";
    private const string BGMVolumeParam = "BGMVolume";
    private const string SFXVolumeParam = "SFXVolume";
    private const string UIVolumeParam = "UIVolume";
    private const float Min_dB = -80f;

    [Header("BGM Data")]
    [SerializeField] private BGMDatabase bgmDatabase;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private int sfxPoolCount = 10;
    [SerializeField] private AudioSource audioSourceSfxPrefab;

    private AudioSource[] sfxSources;
    private int[] sourcePlayIds;
    private int lastPlayId;
    private int[] sourcePriorities;

    private readonly Dictionary<AudioSO, float> lastPlayTimeBySound = new();

    public override void Awake()
    {
        if (Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildSfxPool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayBgm(AudioSO audioDataSO)
    {
        if (audioDataSO == null)
            return;

        AudioClip clip = audioDataSO.GetRandomClip();

        if (clip == null)
            return;

        // 스테이지마다 같은 BGM을 요청해도 처음부터 다시 틀지 않음
        if (bgmSource.isPlaying == true && bgmSource.clip == clip)
            return;

        bgmSource.outputAudioMixerGroup = audioDataSO.AudioMixerGroup != null ? audioDataSO.AudioMixerGroup : bgmMixerGroup;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = audioDataSO.Volume;
        bgmSource.pitch = 1f;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
    }

    public void PlayBgm(RegionType region)
    {
        if (bgmDatabase == null)
            return;

        AudioSO regionBgmDataSO = bgmDatabase.GetBgm(region);

        if (regionBgmDataSO == null)
            return; // 등록 안 된 지역(None 등) 은 지금 BGM유지

        PlayBgm(regionBgmDataSO);
    }

    public void StopBgm()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    // 2D (UI 등 위치 무관)
    public SoundHandle PlaySound(AudioSO audioDataSO) => PlaySound(audioDataSO, Vector3.zero);

    // Transform이 아니라 위치값을 받는다 - 풀에 반납됐거나 파괴된 오브젝트의 Transform이 넘어오는 사고 방지.
    // SpatialBlend가 0인 SO면 위치는 무시됨
    public SoundHandle PlaySound(AudioSO audioDataSO, Vector3 position)
    {
        if (audioDataSO == null)
            return default;

        if (IsPlayedTooRecently(audioDataSO))
            return default; // minInterval 안에 같은 소리가 이미 재생됨 - 겹침

        int sourceIndex = FindSourceIndex(audioDataSO.Priority);
        if (sourceIndex < 0)
            return default;   // 전부 나보다 중요한 소리가 재생 중 - 이번 소리는 버림

        AudioSource audioSource = sfxSources[sourceIndex];
        audioSource.Stop();

        // 클립,그룹,볼륨,피치,루프,우선순위,3D 거리까지 SO 한 곳에서 적용
        if (audioDataSO.ApplyTo(audioSource, sfxMixerGroup) == false)
            return default;

        if (audioDataSO.Is3D)
            audioSource.transform.position = position;

        sourcePriorities[sourceIndex] = audioDataSO.Priority;
        sourcePlayIds[sourceIndex] = ++lastPlayId;
        lastPlayTimeBySound[audioDataSO] = Time.unscaledTime;
        audioSource.Play();

        return new SoundHandle(sourceIndex, sourcePlayIds[sourceIndex]);
    }

    // 같은 SO가 minInterval 안에 다시 요청되면 true
    private bool IsPlayedTooRecently(AudioSO audioDataSO)
    {
        if (audioDataSO.MinInterval <= 0f)
            return false;

        return lastPlayTimeBySound.TryGetValue(audioDataSO, out float lastPlayTime)
            && Time.unscaledTime - lastPlayTime < audioDataSO.MinInterval;
    }

    public void StopSfx(SoundHandle handle)
    {
        if (!handle.IsValid || (uint)handle.Index >= (uint)sfxSources.Length)
            return;

        // 이미 다른 소리가 이 슬롯을 쓰고 있으면 건드리지 않음
        if (sourcePlayIds[handle.Index] != handle.PlayId)
            return;

        sfxSources[handle.Index].Stop();
    }

    // 옵션 UI 슬라이더(0~1)에서 호출. Slider 타입에 의존하지 않도록 값만 받음
    public void SetVolume(AudioChannelType channelType, float linearVolume)
    {
        float clampedVolume = Mathf.Clamp01(linearVolume);
        float volumeDecibel = clampedVolume <= 0.0001f ? Min_dB : Mathf.Log10(clampedVolume) * 20f;

        audioMixer.SetFloat(GetVolumeParam(channelType), volumeDecibel);
    }

    // 슬라이더 초기 위치용. 믹서의 현재 dB 값을 0~1로 변환
    public float GetVolume(AudioChannelType channelType)
    {
        if (!audioMixer.GetFloat(GetVolumeParam(channelType), out float volumeDecibel))
            return 1f;

        return volumeDecibel <= Min_dB ? 0f : Mathf.Clamp01(Mathf.Pow(10f, volumeDecibel / 20f));
    }

    private void BuildSfxPool()
    {
        sfxSources = new AudioSource[sfxPoolCount];
        sourcePlayIds = new int[sfxPoolCount];
        sourcePriorities = new int[sfxPoolCount];

        for (int i = 0; i < sfxPoolCount; ++i)
        {
            AudioSource source;
            if (audioSourceSfxPrefab != null)
                source = Instantiate(audioSourceSfxPrefab, transform);
            else
            {
                GameObject sfxObj = new GameObject("SFX_" + i);   // 초기화 1회뿐이라 문자열 할당 무관
                sfxObj.transform.SetParent(transform, false);
                source = sfxObj.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            sfxSources[i] = source;
        }
    }

    // 1) 쉬는 소스 2) 없으면 나와 같거나 덜 중요한 소스 중 가장 덜 중요한 것 3) 그것도 없으면 -1
    private int FindSourceIndex(int priorityIndex)
    {
        int stealIdIndex = -1;
        int worstPriorityIndex = priorityIndex;

        for (int i = 0; i < sfxSources.Length; ++i)
        {
            AudioSource refSrc = sfxSources[i];
            if (refSrc.isPlaying == false)
                return i;

            if (sourcePriorities[i] >= worstPriorityIndex)
            {
                worstPriorityIndex = sourcePriorities[i];
                stealIdIndex = i;
            }
        }
        return stealIdIndex;
    }

    private static string GetVolumeParam(AudioChannelType type) => type switch
    {
        AudioChannelType.MASTER => MasterVolumeParam,
        AudioChannelType.BGM => BGMVolumeParam,
        AudioChannelType.UI => UIVolumeParam,
        _ => SFXVolumeParam,
    };
}
