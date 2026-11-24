using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Occluder가 붙은 프리팹의 그림자 투사를 한 번에 끈다.
/// 이미 만들어진 프리팹을 정리하는 용도다. 새로 붙이는 Occluder는 Occluder.Reset이 처리한다
/// </summary>
public static class OccluderShadowTool
{
    [MenuItem("Tools/Occluder/프리팹 그림자 일괄 끄기")]
    private static void DisableShadowsInPrefabs()
    {
        int changedCount = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Occluder가 없는 프리팹은 열지 않는다 (열고 저장하는 비용이 크다)
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponentInChildren<Occluder>(true) == null)
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (DisableShadows(root))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedCount++;
                    Debug.Log($"[OccluderShadowTool] 그림자 끔: {path}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Debug.Log($"[OccluderShadowTool] 완료. 변경한 프리팹: {changedCount}개");
    }

    /// <summary>root 아래 Occluder들의 하위 렌더러 그림자를 끈다. 하나라도 바꿨으면 true</summary>
    private static bool DisableShadows(GameObject root)
    {
        bool changed = false;

        foreach (Occluder occluder in root.GetComponentsInChildren<Occluder>(true))
        {
            foreach (Renderer r in occluder.GetComponentsInChildren<Renderer>(true))
            {
                if (r.shadowCastingMode != ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    changed = true;
                }
            }
        }

        return changed;
    }
}
