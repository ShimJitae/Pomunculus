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

    // 문자열 PrefabId → 정수 ID. 같은 문자열이면 빌드/플랫폼/실행에 관계없이 항상 같은 값 (FNV-1a 32bit)
    // string.GetHashCode()는 런타임에 따라 값이 달라질 수 있으므로 통신/세이브에 사용 금지
    // 이 함수의 알고리즘을 바꾸면 기존 세이브 데이터의 ID가 전부 무효가 되므로 절대 수정하지 말 것
    public static int GetHashId(string prefabId)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        uint hash = offsetBasis;
        unchecked
        {
            foreach (char c in prefabId)
            {
                // UTF-16 코드 유닛을 하위/상위 바이트 순으로 처리
                hash = (hash ^ (byte)c) * prime;
                hash = (hash ^ (byte)(c >> 8)) * prime;
            }
        }
        return (int)hash;
    }

    // "프리팹 없음"을 나타내는 해시. 실제 프리팹 이름이 이 값으로 해시되면 ValidateHashIds에서 에러
    public const int NoHash = 0;
    public static int GetHashIdOrNone(string prefabId) => string.IsNullOrEmpty(prefabId) ? NoHash : GetHashId(prefabId);

    // 해시 충돌 검사. 충돌한 그룹마다 errorLog를 호출하고, 충돌이 하나라도 있으면 false
    public static bool ValidateHashIds(IEnumerable<GameObject> entries, System.Action<string> errorLog = null)
    {
        bool ok = true;
        foreach (var go in entries.Where(go => go != null && GetHashId(go.name) == NoHash))
        {
            ok = false;
            errorLog?.Invoke($"[NetworkPrefabCatalog] 프리팹 ID 해시가 예약값(NoHash)과 같음: {go.name}\n프리팹 이름을 변경해야 함");
        }
        var groups = entries
            .Where(go => go != null)
            .GroupBy(go => go.name)               // 이름 중복은 별도 검사에서 처리
            .Select(g => g.First())
            .GroupBy(go => GetHashId(go.name))
            .Where(g => g.Count() > 1);
        foreach (var col in groups)
        {
            ok = false;
            errorLog?.Invoke($"[NetworkPrefabCatalog] 프리팹 ID 해시 충돌 ({col.Key}): {string.Join(", ", col.Select(go => go.name))}\n둘 중 하나의 프리팹 이름을 변경해야 함");
        }
        return ok;
    }

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
            .Where(go => go != null && go.GetComponent<IWorldObj>() != null)
            .Distinct()
            .OrderBy(go => go.name)
            .ToList();

        // PUN은 프리팹 이름으로 생성하므로 이름이 겹치면 안 됨
        foreach (var dup in result.GroupBy(go => go.name).Where(g => g.Count() > 1))
        {
            var paths = string.Join("\n", dup.Select(UnityEditor.AssetDatabase.GetAssetPath));
            Debug.LogError($"[NetworkPrefabCatalog] 프리팹 이름 중복: {dup.Key}\n{paths}", this);
        }

        // 정수 ID(이름 해시)가 겹치면 통신/세이브에서 다른 프리팹으로 오인되므로 반드시 해결해야 함
        ValidateHashIds(result, msg => Debug.LogError(msg, this));

        if (result.SequenceEqual(m_entries)) return;
        m_entries = result;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
