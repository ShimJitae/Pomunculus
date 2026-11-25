using MoreMountains.Tools;
using UnityEngine;

public class EnemyHpBar : MonoBehaviour
{
    [SerializeField] private Enemy owner;
    [SerializeField] private GameObject barObject;
    [SerializeField] private MMProgressBar hpBar;
    [SerializeField] private bool hideWhenFullHp;

    private Transform cameraTransform;

    private void Awake()
    {
        if (owner == null)
            owner = transform.root.GetComponentInChildren<Enemy>();
    }

    private void OnEnable()
    {
        owner.OnHpChanged += HandleHpChanged;
        RefreshBar(owner.CurrentHp, owner.MaxHp, false);   // 활성화·재사용 시 연출 없이 맞춘다
    }

    private void OnDisable()
    {
        owner.OnHpChanged -= HandleHpChanged;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            cameraTransform = mainCamera.transform;
        }

        transform.rotation = cameraTransform.rotation;
    }

    private void HandleHpChanged(int currentHp, int maxHp)
    {
        RefreshBar(currentHp, maxHp, true);
    }

    // 숨겨져 있을 때나 사망 시에는 연출 없이 값만 맞춘다
    private void RefreshBar(int currentHp, int maxHp, bool animate)
    {
        float hpRatio = maxHp > 0 ? (float)currentHp / maxHp : 0f;

        bool isFull = currentHp >= maxHp;
        bool canShow = currentHp > 0 && !(hideWhenFullHp && isFull);

        barObject.SetActive(canShow);   // UpdateBar01은 코루틴을 쓰므로 먼저 켠다

        if (animate && canShow)
        {
            hpBar.UpdateBar01(hpRatio);
        }
        else
        {
            hpBar.SetBar01(hpRatio);
        }
    }
}
