using UnityEngine;

// 씬이 시작되면 지정한 BGM을 튼다. 다른 씬에서 넘어와 이미 다른 BGM이 나오는 중이어도 바꾼다.
public class SceneBgm : MonoBehaviour
{
    [SerializeField] private AudioSO bgm;

    private void Start()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlayBgm(bgm);
    }
}
