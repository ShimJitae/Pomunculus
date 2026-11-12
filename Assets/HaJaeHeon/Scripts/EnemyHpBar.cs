using UnityEngine;
using UnityEngine.UI;

public class EnemyHpBar : MonoBehaviour
{
    [SerializeField] private Enemy owner;
    [SerializeField] private GameObject barObject;
    [SerializeField] private Image fillImage;
    [SerializeField] private bool hideWhenFullHp;

    private Transform cameraTransform;

    private void Awake()
    {
        if (owner == null)
            owner = transform.root.GetComponentInChildren<Enemy>();
    }

    private void OnEnable()
    {
        owner.OnHpChanged += RefreshBar;
        RefreshBar(owner.CurrentHp, owner.MaxHp);
    }

    private void OnDisable()
    {
        owner.OnHpChanged -= RefreshBar;
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

    private void RefreshBar(int currentHp, int maxHp)
    {
        float hpRatio = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        fillImage.fillAmount = hpRatio;

        bool isFull = currentHp >= maxHp;
        bool canShow = currentHp > 0 && !(hideWhenFullHp && isFull);

        barObject.SetActive(canShow);
    }
}
