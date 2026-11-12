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
        public bool IsValid { get; }

        public SoundHandle(int index)
        {
            Index = index;
            IsValid = true;
        }
    }

    private const string MasterVolumeParam = "MasterVolume";
    private const string BGMVolumeParam = "BGMVolume";
    private const string SFXVolumeParam = "SFXVolume";
    private const string UIVolumeParam = "UIVolume";
    private const float Min_DB = -80f;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private int sfxPoolCount = 10;
    [SerializeField] private AudioSource AudioSourceSfxPrefab;

    private AudioSource[] arrSfxSource;
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

    public void PlayBgm(SOAudio so)
    {
        if (so == null)
            return;

        AudioClip clip = so.GetRandomClip();

        if (clip == null)
            return;

        // 스테이지마다 같은 BGM을 요청해도 처음부터 다시 틀지 않음
        if (bgmSource.isPlaying == true && bgmSource.clip == clip)
            return;

        bgmSource.outputAudioMixerGroup = so.OutputGroup;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = so.Volume;
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
    public SoundHandle PlaySound(SOAudio _refAudio) => PlaySound(_refAudio, Vector3.zero);

    // Transform이 아니라 위치값을 받는다 - 풀에 반납됐거나 파괴된 오브젝트의 Transform이 넘어오는 사고 방지.
    // SpatialBlend가 0인 SO면 위치는 무시됨
    public SoundHandle PlaySound(SOAudio so, Vector3 position)
    {
        if (so == null)
            return default;

        AudioClip refClip = so.GetRandomClip();
        if (refClip == null)
            return default;

        int idx = FindSourceIdx(so.Priority);
        if (idx < 0)
            return default;   // 전부 나보다 중요한 소리가 재생 중 - 이번 소리는 버림

        AudioSource audioSource = arrSfxSource[idx];
        audioSource.Stop();
        arrPriority[idx] = so.Priority;

        audioSource.clip = refClip;
        audioSource.outputAudioMixerGroup = so.OutputGroup;
        audioSource.volume = so.Volume;
        audioSource.pitch = so.GetRandomPitch();
        audioSource.loop = so.Loop;
        audioSource.priority = so.Priority;      // Unity 자체 보이스 컬링도 같은 기준으로
        audioSource.spatialBlend = so.SpatialBlend;
        if (so.SpatialBlend > 0.0f)
            audioSource.transform.position = position;

        audioSource.Play();
        return new SoundHandle(idx);
    }

    public void StopSfx(SoundHandle handle)
    {
        if (!handle.IsValid || (uint)handle.Index >= (uint)arrSfxSource.Length)
            return;

        // shortcut: 재사용 후에는 현재 소리를 정지하므로, 재생별 식별이 필요해지면 보완.
        arrSfxSource[handle.Index].Stop();
    }

    // 옵션 UI 슬라이더(0~1)에서 호출. Slider 타입에 의존하지 않도록 값만 받음
    public void SetVolume(AudioChannelType type, float _fLinear) //0~1
    {
        float fDB = _fLinear <= 0.0001f ? Min_DB : Mathf.Log10(_fLinear) * 20.0f;
        audioMixer.SetFloat(GetVolumeParam(type), fDB);
    }

    private void BuildSfxPool()
    {
        arrSfxSource = new AudioSource[sfxPoolCount];
        arrPriority = new int[sfxPoolCount];

        for (int i = 0; i < sfxPoolCount; ++i)
        {
            AudioSource refSrc;
            if (AudioSourceSfxPrefab != null)
                refSrc = Instantiate(AudioSourceSfxPrefab, transform);
            else
            {
                GameObject refObj = new GameObject("SFX_" + i);   // 초기화 1회뿐이라 문자열 할당 무관
                refObj.transform.SetParent(transform, false);
                refSrc = refObj.AddComponent<AudioSource>();
            }

            refSrc.playOnAwake = false;
            refSrc.loop = false;
            arrSfxSource[i] = refSrc;
        }
    }

    // 1) 쉬는 소스 2) 없으면 나와 같거나 덜 중요한 소스 중 가장 덜 중요한 것 3) 그것도 없으면 -1
    private int FindSourceIdx(int _iPriority)
    {
        int iStealIdx = -1;
        int iWorstPriority = _iPriority;

        for (int i = 0; i < arrSfxSource.Length; ++i)
        {
            AudioSource refSrc = arrSfxSource[i];
            if (refSrc.isPlaying == false)
                return i;

            if (arrPriority[i] >= iWorstPriority)
            {
                iWorstPriority = arrPriority[i];
                iStealIdx = i;
            }
        }
        return iStealIdx;
    }

    private static string GetVolumeParam(AudioChannelType _eType) => _eType switch
    {
        AudioChannelType.MASTER => MasterVolumeParam,
        AudioChannelType.BGM => BGMVolumeParam,
        AudioChannelType.UI => UIVolumeParam,
        _ => SFXVolumeParam,
    };
}
