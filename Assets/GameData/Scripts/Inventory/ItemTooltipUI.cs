using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ItemTooltipUI : MonoBehaviour
    {
        [Header("표시할 UI")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text amountText;
        [Header("원소 수치")]
        [SerializeField] private TMP_Text physicsText;
        [SerializeField] private TMP_Text fireText;
        [SerializeField] private TMP_Text coldText;
        [SerializeField] private TMP_Text windText;

        [Header("위치")]
        [SerializeField] private Vector2 mouseOffset = new Vector2(20, 20);
        [SerializeField] private float edgePadding = 15;

        private RectTransform tooltipRect;
        private RectTransform canvasRect;
        private Canvas rootCanvas;
        private InventorySlotUI owner;

        public void Show(InventorySlotUI slot, string itemName, string description, Sprite sprite, int amount, Vector2 screenPosition,
            float physics, float fire, float cold, float wind)
        {
            tooltipRect = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            canvasRect = rootCanvas.GetComponent<RectTransform>();

            CanvasGroup group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            owner = slot;

            nameText.text = itemName;
            descriptionText.text = description;
            amountText.text = $"보유 수량: {amount}개";
            physicsText.text = $"×{physics:0.##}";
            fireText.text = $"×{fire:0.##}";
            coldText.text = $"×{cold:0.##}";
            windText.text = $"×{wind:0.##}";

            icon.sprite = sprite;
            icon.preserveAspect = true;

            tooltipRect.pivot = new Vector2(0, 1);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            UpdatePosition(screenPosition);
        }

        private void LateUpdate()
        {
            if (Mouse.current != null)
                UpdatePosition(Mouse.current.position.ReadValue());
        }

        private void UpdatePosition(Vector2 screenPosition)
        {
            Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out Vector2 mousePosition))
                return;

            Rect bounds = canvasRect.rect;

            float width = tooltipRect.rect.width * tooltipRect.localScale.x;
            float height = tooltipRect.rect.height * tooltipRect.localScale.y;

            float x = mousePosition.x + mouseOffset.x;
            float y = mousePosition.y - mouseOffset.y;

            // 오른쪽 공간이 부족하면 마우스 왼쪽으로 이동합니다.
            if (x + width > bounds.xMax - edgePadding)
                x = mousePosition.x - mouseOffset.x - width;

            // 아래쪽 공간이 부족하면 마우스 위쪽으로 이동합니다.
            if (y - height < bounds.yMin + edgePadding)
                y = mousePosition.y + mouseOffset.y + height;

            float minX = bounds.xMin + edgePadding;
            float maxX = bounds.xMax - edgePadding - width;
            float minY = bounds.yMin + edgePadding + height;
            float maxY = bounds.yMax - edgePadding;

            x = maxX >= minX ? Mathf.Clamp(x, minX, maxX) : minX;
            y = maxY >= minY ? Mathf.Clamp(y, minY, maxY) : maxY;

            tooltipRect.localPosition = new Vector3(x, y, 0);
        }

        public void HideFor(InventorySlotUI slot)
        {
            if (owner == slot)
                Hide();
        }

        public void Hide()
        {
            owner = null;
            gameObject.SetActive(false);
        }
    }
}