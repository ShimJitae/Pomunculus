using UnityEngine;

// 오브젝트에 붙여서 AudioSo를 재생하는 컴포넌트
// 인스펙터(UnityEvent), 애니메이션 이벤트에서 호출하고, 루프 소리는 꺼질 때 자동으로 정지한다.
public class AudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioSO audioDataSO; // 기본으로 낼 소리
    [SerializeField] private bool playOnEnable; // 켜질 때 자동 재생 (풀 재사용 포함) Play on Awake 랑 같음

    private AudioManager.SoundHandle loopHandle; // 재생중인 루프 소리 (끌 때 사용)
    private bool hasStarted; // 첫 활성화는 Start에서 재생했는지

    private void Start()
    {
        hasStarted = true;

        if (playOnEnable)
            Play();
    }

    private void OnEnable()
    {
        if (hasStarted && playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }
    
    // 기본 소리 재생(UnityEvent, 애니메이션 이벤트용)
    public void Play()
    {
        PlayAudio(audioDataSO);
    }

    public void PlayAudio(AudioSO otherAudioDataSo)
    {
        if (otherAudioDataSo == null || AudioManager.Instance == null)
            return;

        if(otherAudioDataSo.Loop)
        {
            Stop(); // 이전 루프가 남아 있으면 정리
            loopHandle = AudioManager.Instance.PlaySound(otherAudioDataSo, transform.position);
            
            return;
        }

        AudioManager.Instance.PlaySound(otherAudioDataSo, transform.position);
    }

    // 루프 소리 끄기
    public void Stop()
    {
        if (!loopHandle.IsValid)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopSfx(loopHandle);

        loopHandle = default;
    }
}
