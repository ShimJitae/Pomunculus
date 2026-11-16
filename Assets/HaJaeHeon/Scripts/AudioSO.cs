using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/*///////////////////////////////////////////
                    SOAudio
목적 : 소리 한 종류의 재생 데이터 (클립 변형, 채널, 볼륨, 피치 범위, 3D 정도, 우선순위, 루프)
 *///////////////////////////////////////////
[CreateAssetMenu(menuName = "Scriptable Objects/AudioSO")]
public sealed class AudioSO : ScriptableObject
{
    [Header("클립")]
    [SerializeField] private List<AudioClip> clipList = new();          // 같은 소리의 변형들 - 랜덤 재생으로 반복감 감소

    [Header("출력")]
    [SerializeField] private AudioMixerGroup audioMixerGroup;           // 비우면 AudioManager의 기본 그룹 사용
    [SerializeField, Range(0f, 1f)] private float volume = 1.0f;
    [SerializeField, Range(0.1f, 3f)] private float pitchMin = 1.0f;    // 연속 재생 시 기계적인 느낌 완화
    [SerializeField, Range(0.1f, 3f)] private float pitchMax = 1.0f;
    [SerializeField, Range(0, 256)] private int priority = 128;         // 낮을수록 중요 (AudioSource.priority와 같은 의미)
    [SerializeField] private bool loop = false;

    [Header("3D (Spatial Blend > 0일 때만 사용)")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.0f;  // 0 = 2D(플레이어 무기/UI), 1 = 3D(몬스터)
    [SerializeField, Min(0f)] private float minDistance = 1f;           // 이 거리까지는 최대 볼륨
    [SerializeField, Min(0f)] private float maxDistance = 20f;          // 이 거리에서 0 (Linear 기준)
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    [Header("연타 제한")]
    [SerializeField, Min(0f)] private float minInterval = 0.05f;        // 같은 소리 재생 최소 간격(초). 0이면 제한 없음

    public AudioMixerGroup AudioMixerGroup => audioMixerGroup;
    public float Volume => volume;
    public float SpatialBlend => spatialBlend;
    public int Priority => priority;
    public bool Loop => loop;
    public bool Is3D => spatialBlend > 0f;
    public float MinInterval => minInterval;

    private int lastClipIndex = -1;   // 직전에 뽑은 클립 (같은 클립 연속 방지)

    public AudioClip GetRandomClip()
    {
        if (clipList.Count == 0)
            return null;

        if (clipList.Count == 1)
            return clipList[0];

        int clipIndex;

        if (lastClipIndex < 0)
        {
            clipIndex = Random.Range(0, clipList.Count);
        }
        else
        {
            // 직전 인덱스를 뺀 나머지 중에서 뽑음
            clipIndex = Random.Range(0, clipList.Count - 1);

            if (clipIndex >= lastClipIndex)
                clipIndex++;
        }

        lastClipIndex = clipIndex;
        return clipList[clipIndex];
    }
    public float GetRandomPitch() => Random.Range(pitchMin, pitchMax);

    public bool ApplyTo(AudioSource audioSource, AudioMixerGroup fallbackGroup = null)
    {
        AudioClip clip = GetRandomClip();

        if (audioSource == null || clip == null)
            return false;

        audioSource.clip = clip;
        audioSource.outputAudioMixerGroup = audioMixerGroup != null ? audioMixerGroup : fallbackGroup;
        audioSource.volume = volume;
        audioSource.pitch = GetRandomPitch();
        audioSource.loop = loop;
        audioSource.priority = priority;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = rolloffMode;

        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (clipList.Count == 0)
            Debug.LogWarning($"{name}: 클립이 없습니다.", this);

        if (pitchMin > pitchMax)
            Debug.LogWarning($"{name}: pitchMin({pitchMin})이 pitchMax({pitchMax})보다 큽니다.", this);

        if (spatialBlend > 0f && maxDistance <= minDistance)
            Debug.LogWarning($"{name}: maxDistance가 minDistance보다 커야 합니다.", this);
    }
#endif
}