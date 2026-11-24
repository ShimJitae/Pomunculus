using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pomunculus.Items
{
    public class CraftHistoryRowUI : MonoBehaviour
    {
        [Header("재료 데이터")]
        [SerializeField] private MaterialDatabase materialDatabase;

        [Header("재료 아이콘 3개")]
        [SerializeField] private Image[] materialIcons = new Image[3];

        public void Show(IReadOnlyList<int> materialIds)
        {
            if (materialIds == null || materialIds.Count == 0)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);

            for (int i = 0; i < materialIcons.Length; i++)
            {
                Image icon = materialIcons[i];

                if (icon == null)
                    continue;

                MaterialData material = null;

                if (i < materialIds.Count && materialDatabase != null)
                    material = materialDatabase.Find(materialIds[i]);

                icon.sprite = material != null ? material.Icon : null;
                icon.preserveAspect = true;
                icon.enabled = icon.sprite != null;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}