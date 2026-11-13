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

        public SoundHandle(int index, int id)
        {
            Index = index;
            PlayId = id;
        }
    }

    private const string MasterVolumeParam = "MasterVolume";
    private const string BGMVolumeParam = "BGMVolume";
    private const string SFXVolumeParam = "SFXVolume";
    private const string UIVolumeParam = "UIVolume";
    private const float Min_dB = -80f;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private int sfxPoolCount = 10;
    [SerializeField] private AudioSource AudioSourceSfxPrefab;

    private AudioSource[] arrSfxSource;
    private int[] arrPlayId;
    private int lastPlayId;
    private int[] arrPriority;

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

        bgmSource.outputAudioMixerGroup = audioDataSO.AudioMixerGroup;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = audioDataSO.Volume;
        bgmSource.pitch = 1f;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
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
    public SoundHandle PlaySound(AudioSO audioDataSo, Vector3 position)
    {
        if (audioDataSo == null)
            return default;

        //AudioClip refClip = audioDataSo.GetRandomClip();
        //if (refClip == null)
        //    return default;

        int sourceIndex = FindSourceIndex(audioDataSo.Priority);
        if (sourceIndex < 0)
            return default;   // 전부 나보다 중요한 소리가 재생 중 - 이번 소리는 버림

        AudioSource audioSource = arrSfxSource[sourceIndex];
        audioSource.Stop();

        // 클립,그룹,볼륨,피치,루프,우선순위,3D 거리까지 SO 한 곳에서 적용
        if (audioDataSo.ApplyTo(audioSource, sfxMixerGroup) == false)
            return default;

        if (audioDataSo.Is3D)
            audioSource.transform.position = position;

        arrPriority[sourceIndex] = audioDataSo.Priority;
        arrPlayId[sourceIndex] = ++lastPlayId;
        audioSource.Play();

        return new SoundHandle(sourceIndex, arrPlayId[sourceIndex]);
    }

    public void StopSfx(SoundHandle handle)
    {
        if (!handle.IsValid || (uint)handle.Index >= (uint)arrSfxSource.Length)
            return;

        // 이미 다른 소리가 이 슬롯을 쓰고 있으면 건드리지 않음
        if (arrPlayId[handle.Index] != handle.PlayId)
            return;

        arrSfxSource[handle.Index].Stop();
    }

    // 옵션 UI 슬라이더(0~1)에서 호출. Slider 타입에 의존하지 않도록 값만 받음
    public void SetVolume(AudioChannelType type, float linear) //0~1
    {
        float sound_dB = linear <= 0.0001f ? Min_dB : Mathf.Log10(linear) * 20.0f;
        audioMixer.SetFloat(GetVolumeParam(type), sound_dB);
    }

    private void BuildSfxPool()
    {
        arrSfxSource = new AudioSource[sfxPoolCount];
        arrPlayId = new int[sfxPoolCount];
        arrPriority = new int[sfxPoolCount];

        for (int i = 0; i < sfxPoolCount; ++i)
        {
            AudioSource source;
            if (AudioSourceSfxPrefab != null)
                source = Instantiate(AudioSourceSfxPrefab, transform);
            else
            {
                GameObject sfxObj = new GameObject("SFX_" + i);   // 초기화 1회뿐이라 문자열 할당 무관
                sfxObj.transform.SetParent(transform, false);
                source = sfxObj.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            arrSfxSource[i] = source;
        }
    }

    // 1) 쉬는 소스 2) 없으면 나와 같거나 덜 중요한 소스 중 가장 덜 중요한 것 3) 그것도 없으면 -1
    private int FindSourceIndex(int priorityIndex)
    {
        int stealIdIndex = -1;
        int worstPriorityIndex = priorityIndex;

        for (int i = 0; i < arrSfxSource.Length; ++i)
        {
            AudioSource refSrc = arrSfxSource[i];
            if (refSrc.isPlaying == false)
                return i;

            if (arrPriority[i] >= worstPriorityIndex)
            {
                worstPriorityIndex = arrPriority[i];
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
