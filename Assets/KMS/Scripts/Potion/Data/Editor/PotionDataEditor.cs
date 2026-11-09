using System;
using UnityEditor;
using UnityEngine;

namespace Pomunculus.Items.Editor
{
    // PotionData 인스펙터에 효과 종류를 골라 추가하는 버튼을 붙인다
    [CustomEditor(typeof(PotionData))]
    public class PotionDataEditor : UnityEditor.Editor
    {
        // 기본 인스펙터를 그린 뒤 효과 추가 버튼을 그린다
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("효과 추가"))
                ShowAddEffectMenu();
        }

        // PotionEffect를 상속한 모든 클래스를 메뉴로 보여 준다
        private void ShowAddEffectMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<PotionEffect>())
            {
                if (type.IsAbstract) continue;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () => AddEffect(type));
            }

            menu.ShowAsContext();
        }

        // 선택한 종류의 효과를 리스트 끝에 새로 만들어 넣는다
        private void AddEffect(Type type)
        {
            serializedObject.Update();
            SerializedProperty effects = serializedObject.FindProperty("effects");

            effects.arraySize++;
            // 크기만 늘리면 이전 원소를 같은 참조로 복사하므로 반드시 새 객체로 덮어쓴다
            effects.GetArrayElementAtIndex(effects.arraySize - 1).managedReferenceValue = Activator.CreateInstance(type);

            serializedObject.ApplyModifiedProperties();
        }
    }
}