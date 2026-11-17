using System;
using UnityEditor;
using UnityEngine;

namespace Pomunculus.Items.Editor
{
    // [PotionEffectPicker]가 붙은 리스트 항목마다 효과 종류를 고르는 드롭다운을 그린다
    [CustomPropertyDrawer(typeof(PotionEffectPickerAttribute))]
    public class PotionEffectPickerDrawer : PropertyDrawer
    {
        private const string EmptyLabel = "(효과 선택)";

        // 펼쳤을 때 자식 필드까지 포함한 높이를 돌려준다
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, true);

        // 첫 줄 오른쪽에 종류 선택 버튼을, 그 아래에 기본 필드를 그린다
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Rect buttonRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);

            // 버튼을 필드보다 먼저 그려야 클릭이 접기 화살표에 먹히지 않는다
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(GetTypeLabel(property)), FocusType.Keyboard))
                ShowEffectTypeMenu(property);

            EditorGUI.PropertyField(position, property, label, true);
        }

        // 현재 들어 있는 효과 종류 이름. 비어 있으면 안내 문구
        private static string GetTypeLabel(SerializedProperty property)
        {
            object current = property.managedReferenceValue;
            return current == null ? EmptyLabel : ObjectNames.NicifyVariableName(current.GetType().Name);
        }

        // 고를 수 있는 효과 종류를 메뉴로 보여 준다
        private void ShowEffectTypeMenu(SerializedProperty property)
        {
            PotionEffectPickerAttribute picker = (PotionEffectPickerAttribute)attribute;
            SerializedObject serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath;   // 메뉴 콜백은 나중에 불리므로 경로만 기억한다
            Type currentType = property.managedReferenceValue?.GetType();

            GenericMenu menu = new GenericMenu();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<PotionEffect>())
            {
                if (type.IsAbstract) continue;
                if (!picker.AllowZone && type == typeof(ZonePotionEffect)) continue;

                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), type == currentType,
                    () => SetEffect(serializedObject, propertyPath, type));
            }
            menu.ShowAsContext();
        }

        // 선택한 종류의 새 효과 객체를 그 항목에 넣는다
        private static void SetEffect(SerializedObject serializedObject, string propertyPath, Type type)
        {
            serializedObject.Update();
            serializedObject.FindProperty(propertyPath).managedReferenceValue = Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();   // Undo(Ctrl+Z)도 자동으로 기록된다
        }
    }
}