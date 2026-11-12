using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SOAudio", menuName = "Scriptable Objects/SOAudio")]
public class SOAudio : ScriptableObject
{
    [SerializeField] private List<AudioClip> clipList = new();          // 같은 소리의 변형들 - 랜덤 재생으로 반복감 감소
    [SerializeField] private AudioMixerGroup audioMixerGroup;
    [SerializeField, Range(0f, 1f)] private float volume = 1.0f;
    [SerializeField, Range(0.1f, 3f)] private float pitchMin = 1.0f;    // 연속 재생 시 기계적인 느낌 완화
    [SerializeField, Range(0.1f, 3f)] private float pitchMax = 1.0f;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.0f;  // 0 = 2D(플레이어 무기/UI), 1 = 3D(몬스터)
    [SerializeField, Range(0, 256)] private int priority = 128;         // 낮을수록 중요 (AudioSource.priority와 같은 의미)
    [SerializeField] private bool loop = false;

    public AudioMixerGroup OutputGroup => audioMixerGroup;
    public float Volume => volume;
    public float SpatialBlend => spatialBlend;
    public int Priority => priority;
    public bool Loop => loop;

    public AudioClip GetRandomClip()
    {
        if (clipList.Count == 0)
            return null;

        return clipList[Random.Range(0, clipList.Count)];
    }

    public float GetRandomPitch()
    {
        return Random.Range(pitchMin, pitchMax);
    }
}
