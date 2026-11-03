using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu]
public class NetworkPrefabCatalog : ScriptableObject
{
    // 인스펙터에 폴더를 드래그해서 지정. GUID로 참조하므로 폴더 이름/위치가 바뀌어도 유지됨
    [SerializeField] private List<Object> m_folders = new();
    // 폴더 밖에 있는 프리팹을 개별로 추가할 때 사용
    [SerializeField] private List<GameObject> m_extraPrefabs = new();
    // Refresh로 자동 생성되는 목록. 직접 수정하지 말 것
    [SerializeField, FormerlySerializedAs("Entries")] private List<GameObject> m_entries = new();

    public IReadOnlyList<GameObject> Entries => m_entries;

#if UNITY_EDITOR
    public IEnumerable<string> FolderPaths => m_folders
        .Where(f => f != null)
        .Select(UnityEditor.AssetDatabase.GetAssetPath)
        .Where(UnityEditor.AssetDatabase.IsValidFolder);

    private void OnValidate()
    {
        // 폴더가 아닌 에셋이 들어오면 제거
        for (int i = 0; i < m_folders.Count; i++)
        {
            var f = m_folders[i];
            if (f != null && !UnityEditor.AssetDatabase.IsValidFolder(UnityEditor.AssetDatabase.GetAssetPath(f)))
            {
                Debug.LogWarning($"[NetworkPrefabCatalog] 폴더가 아닙니다: {f.name}", this);
                m_folders[i] = null;
            }
        }
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        var folders = FolderPaths.ToArray();
        var fromFolders = folders.Length == 0
            ? Enumerable.Empty<GameObject>()
            : UnityEditor.AssetDatabase.FindAssets("t:Prefab", folders)
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>);

        var result = fromFolders
            .Concat(m_extraPrefabs)
            .Where(go => go != null && go.GetComponent<PhotonView>() != null)
            .Distinct()
            .OrderBy(go => go.name)
            .ToList();

        // PUN은 프리팹 이름으로 생성하므로 이름이 겹치면 안 됨
        foreach (var dup in result.GroupBy(go => go.name).Where(g => g.Count() > 1))
        {
            var paths = string.Join("\n", dup.Select(UnityEditor.AssetDatabase.GetAssetPath));
            Debug.LogError($"[NetworkPrefabCatalog] 프리팹 이름 중복: {dup.Key}\n{paths}", this);
        }

        if (result.SequenceEqual(m_entries)) return;
        m_entries = result;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
