using UnityEngine;
using UnityEngine.UI;

// 볼륨 슬라이더에 붙여서 AudioManager의 채널 볼륨과 연결한다
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private AudioChannelType channelType;   // 조절할 채널 (Master, BGM, SFX, UI)

    private Slider volumeSlider;

    private void Awake()
    {
        volumeSlider = GetComponent<Slider>();

        // SetVolume이 0~1 값을 받으므로 범위를 고정
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
    }

    // 옵션 창이 열릴 때마다 현재 볼륨 위치로 맞춤
    private void OnEnable()
    {
        volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);

        if (AudioManager.Instance != null)
            volumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(channelType));
    }

    private void OnDisable()
    {
        volumeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float sliderValue)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetVolume(channelType, sliderValue);
    }
}