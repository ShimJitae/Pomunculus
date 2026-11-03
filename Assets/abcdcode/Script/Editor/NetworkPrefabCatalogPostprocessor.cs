using System.Linq;
using UnityEditor;

// 카탈로그에 지정된 폴더 안에서 프리팹이 추가/삭제/이동되면 자동으로 Refresh
public class NetworkPrefabCatalogPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        var changed = imported.Concat(deleted).Concat(moved).Concat(movedFrom)
            .Where(p => p.EndsWith(".prefab"))
            .ToArray();
        if (changed.Length == 0) return;
        bool prefabDeleted = deleted.Any(p => p.EndsWith(".prefab"));

        foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(NetworkPrefabCatalog)}"))
        {
            var catalog = AssetDatabase.LoadAssetAtPath<NetworkPrefabCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            if (catalog == null) continue;

            var folders = catalog.FolderPaths.Select(f => f + "/").ToArray();
            // 개별 추가한 프리팹이 삭제된 경우도 반영해야 하므로, 폴더 밖 변경이라도 삭제는 항상 갱신
            if (prefabDeleted ||changed.Any(p => folders.Any(p.StartsWith)))
                catalog.Refresh();
        }
    }
}
