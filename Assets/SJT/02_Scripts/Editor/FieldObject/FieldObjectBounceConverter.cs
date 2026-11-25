using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 채집물 프리팹에 스폰 바운스 구조를 적용한다.
/// 루트의 메시(또는 기존 자식)를 SquashContainer 아래로 옮기고, 생성 시 자동 재생되는 공용 SpawnFeedback을 붙인다.
/// 루트의 위치·스케일·콜라이더·스크립트는 건드리지 않는다. 이미 변환된 프리팹은 건너뛴다.
/// </summary>
public static class FieldObjectBounceConverter
{
    private const string FieldObjectFolder = "Assets/FildOb";
    private const string SpawnFeedbackPrefabPath = "Assets/SJT/03_Prefabs/Feedback/FieldObjectSpawnFeedback.prefab";
    private const string ContainerName = "SquashContainer";
    private const string ModelName = "Model";
    private const string SpawnFeedbackName = "SpawnFeedback";
    private const float BoundsTolerance = 0.001f;

    [MenuItem("Tools/Pomunculus/FieldObject Bounce/선택한 프리팹 변환")]
    private static void ConvertSelected()
    {
        string[] paths = Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(p => p.EndsWith(".prefab"))
            .ToArray();
        Run(paths);
    }

    [MenuItem("Tools/Pomunculus/FieldObject Bounce/FildOb 전체 변환")]
    private static void ConvertAll()
    {
        Run(FindFieldObjectPrefabs());
    }

    [MenuItem("Tools/Pomunculus/FieldObject Bounce/FildOb 전체 구조 검사")]
    private static void VerifyAll()
    {
        StringBuilder log = new StringBuilder();
        int failed = 0;
        string[] paths = FindFieldObjectPrefabs();
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string error = VerifyStructure(prefab);
            if (error != null)
            {
                failed++;
                log.AppendLine($"  실패 {path} : {error}");
            }
        }
        Debug.Log($"[FieldObjectBounce] 구조 검사 {paths.Length}개 중 실패 {failed}개\n{log}");
    }

    private static string[] FindFieldObjectPrefabs()
    {
        return AssetDatabase.FindAssets("t:Prefab", new[] { FieldObjectFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .ToArray();
    }

    private static void Run(string[] paths)
    {
        GameObject spawnFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpawnFeedbackPrefabPath);
        if (spawnFeedbackPrefab == null)
        {
            Debug.LogError($"[FieldObjectBounce] 공용 연출 프리팹 없음 : {SpawnFeedbackPrefabPath}");
            return;
        }

        List<string> converted = new List<string>();
        List<string> skipped = new List<string>();
        List<string> failed = new List<string>();

        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                EditorUtility.DisplayProgressBar("FieldObject Bounce", paths[i], (float)i / paths.Length);
                string result = ConvertPrefab(paths[i], spawnFeedbackPrefab);
                if (result == null) converted.Add(paths[i]);
                else if (result == "skip") skipped.Add(paths[i]);
                else failed.Add($"{paths[i]} : {result}");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        StringBuilder log = new StringBuilder();
        log.AppendLine($"[FieldObjectBounce] 변환 {converted.Count} / 건너뜀 {skipped.Count} / 실패 {failed.Count}");
        foreach (string f in failed) log.AppendLine("  실패 " + f);
        foreach (string s in skipped) log.AppendLine("  건너뜀 " + s);
        if (failed.Count > 0) Debug.LogError(log.ToString());
        else Debug.Log(log.ToString());
    }

    /// <summary>성공이면 null, 이미 변환됐으면 "skip", 실패면 사유를 돌려준다. 실패하면 저장하지 않는다.</summary>
    private static string ConvertPrefab(string path, GameObject spawnFeedbackPrefab)
    {
        // FildOb 프리팹의 Variant는 베이스 변환을 물려받는다. 자동 변환하면 컨테이너가 중복되거나
        // 루트 렌더러에 걸린 머티리얼 오버라이드가 사라지므로 수동 확인으로 돌린다
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(asset);
        if (basePrefab != null && AssetDatabase.GetAssetPath(basePrefab).StartsWith(FieldObjectFolder))
        {
            return asset.transform.Find(ContainerName) != null
                ? "skip"
                : $"FildOb 프리팹({AssetDatabase.GetAssetPath(basePrefab)})의 Variant. 베이스를 먼저 변환한 뒤 머티리얼 오버라이드를 Model에 수동 적용 필요";
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.transform.Find(ContainerName) != null)
                return "skip";

            Entity entity = root.GetComponent<Entity>();
            MeshFilter meshFilter = root.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = root.GetComponent<MeshRenderer>();
            if (entity == null) return "루트에 Entity 없음";
            if (entity.GetType() != typeof(Entity)) return $"예상 밖 Entity 타입 {entity.GetType().Name}";

            // 메시가 루트에 있으면 Model로 옮기고, 자식(LOD 그룹 등)에 있으면 자식들을 통째로 컨테이너 아래로 옮긴다
            bool meshOnRoot = meshFilter != null && meshRenderer != null;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (meshOnRoot && renderers.Length != 1) return "루트 메시 외에 다른 렌더러가 있음";
            if (!meshOnRoot)
            {
                if (root.GetComponent<Renderer>() != null || meshFilter != null) return "루트 메시 구성이 불완전함";
                if (renderers.Length == 0) return "렌더러 없음";
                if (PrefabUtility.IsPartOfPrefabInstance(root)) return "Variant 루트의 자식은 옮길 수 없음";
            }

            RendererSnapshot before = meshOnRoot ? RendererSnapshot.Capture(meshFilter, meshRenderer) : default;
            Bounds[] childBoundsBefore = renderers.Select(r => r.bounds).ToArray();
            int childCountBefore = root.transform.childCount;

            // 1. SquashContainer 생성
            GameObject container = CreateChild(ContainerName, root.transform, root);

            MeshFilter newFilter = null;
            MeshRenderer newRenderer = null;
            if (meshOnRoot)
            {
                // 2-A. 메시를 Model로 복사한 뒤 루트에서 제거
                GameObject model = CreateChild(ModelName, container.transform, root);
                newFilter = model.AddComponent<MeshFilter>();
                newRenderer = model.AddComponent<MeshRenderer>();
                if (!CopyComponentValues(meshFilter, newFilter) || !CopyComponentValues(meshRenderer, newRenderer))
                    return "메시 컴포넌트 값 복사 실패";

                Object.DestroyImmediate(meshRenderer);
                Object.DestroyImmediate(meshFilter);
                if (root.GetComponent<MeshRenderer>() != null || root.GetComponent<MeshFilter>() != null)
                    return "루트 메시 컴포넌트 제거 실패";
            }
            else
            {
                // 2-B. 기존 자식을 순서대로 컨테이너 아래로 옮긴다. 컨테이너가 기본 트랜스폼이라 로컬 값 그대로 월드 위치가 유지된다
                List<Transform> children = new List<Transform>();
                foreach (Transform child in root.transform)
                {
                    if (child != container.transform) children.Add(child);
                }
                if (children.Count != childCountBefore) return "자식 수 불일치";
                foreach (Transform child in children)
                {
                    child.SetParent(container.transform, false);
                }
            }

            // 3. 공용 SpawnFeedback 배치 (켜지는 순간 자동 재생되므로 코드 연결이 필요 없다)
            GameObject spawnFeedback = (GameObject)PrefabUtility.InstantiatePrefab(spawnFeedbackPrefab, container.transform);
            spawnFeedback.name = SpawnFeedbackName;
            spawnFeedback.transform.localPosition = Vector3.zero;
            spawnFeedback.transform.localRotation = Quaternion.identity;
            spawnFeedback.transform.localScale = Vector3.one;

            // 4. 외형이 그대로인지 비교
            if (meshOnRoot)
            {
                string diff = before.Compare(RendererSnapshot.Capture(newFilter, newRenderer));
                if (diff != null) return "외형 불일치 : " + diff;
            }
            else
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (!RendererSnapshot.SameBounds(childBoundsBefore[i], renderers[i].bounds))
                        return $"외형 불일치 : {renderers[i].name} bounds";
                }
            }

            string structureError = VerifyStructure(root);
            if (structureError != null) return structureError;

            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            return success ? null : "저장 실패";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject CreateChild(string name, Transform parent, GameObject layerSource)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        child.layer = layerSource.layer;
        child.tag = layerSource.tag;
        return child;
    }

    private static bool CopyComponentValues(Component source, Component target)
    {
        return UnityEditorInternal.ComponentUtility.CopyComponent(source)
            && UnityEditorInternal.ComponentUtility.PasteComponentValues(target);
    }

    private static string VerifyStructure(GameObject root)
    {
        if (root == null) return "프리팹 로드 실패";
        if (root.GetComponent<Renderer>() != null || root.GetComponent<MeshFilter>() != null) return "루트에 메시가 남아 있음";

        Entity entity = root.GetComponent<Entity>();
        if (entity == null || entity.GetType() != typeof(Entity)) return "루트가 Entity가 아님";

        Transform container = root.transform.Find(ContainerName);
        if (container == null) return "SquashContainer 없음";
        if (container.localScale != Vector3.one || container.localPosition != Vector3.zero || container.localRotation != Quaternion.identity)
            return "SquashContainer 트랜스폼이 기본값이 아님";

        // 모든 렌더러가 컨테이너 아래에 있어야 함께 바운스된다
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return "렌더러 없음";
        if (renderers.Any(r => !r.transform.IsChildOf(container))) return "컨테이너 밖에 렌더러가 있음";

        Transform model = container.Find(ModelName);
        if (model != null)
        {
            if (model.GetComponent<MeshRenderer>() == null || model.GetComponent<MeshFilter>() == null) return "Model 메시 없음";
            if (renderers.Length != 1) return "렌더러 수가 1이 아님";
        }

        Transform spawnFeedback = container.Find(SpawnFeedbackName);
        MMF_Player player = spawnFeedback != null ? spawnFeedback.GetComponent<MMF_Player>() : null;
        if (player == null) return "SpawnFeedback 없음";
        if (!player.AutoPlayOnEnable) return "SpawnFeedback 자동 재생 꺼짐";
        return null;
    }

    /// <summary>변환 전후 외형 비교용 값</summary>
    private struct RendererSnapshot
    {
        public Mesh Mesh;
        public Material[] Materials;
        public UnityEngine.Rendering.ShadowCastingMode ShadowCasting;
        public bool ReceiveShadows;
        public bool Enabled;
        public Bounds WorldBounds;

        public static RendererSnapshot Capture(MeshFilter filter, MeshRenderer renderer)
        {
            return new RendererSnapshot
            {
                Mesh = filter.sharedMesh,
                Materials = renderer.sharedMaterials,
                ShadowCasting = renderer.shadowCastingMode,
                ReceiveShadows = renderer.receiveShadows,
                Enabled = renderer.enabled,
                WorldBounds = renderer.bounds,
            };
        }

        public string Compare(RendererSnapshot other)
        {
            if (Mesh != other.Mesh) return "메시";
            if (!Materials.SequenceEqual(other.Materials)) return "머티리얼";
            if (ShadowCasting != other.ShadowCasting || ReceiveShadows != other.ReceiveShadows) return "그림자 설정";
            if (Enabled != other.Enabled) return "렌더러 활성 상태";
            if (!SameBounds(WorldBounds, other.WorldBounds))
                return $"bounds {WorldBounds} → {other.WorldBounds}";
            return null;
        }

        public static bool SameBounds(Bounds a, Bounds b)
        {
            return Vector3.Distance(a.center, b.center) <= BoundsTolerance
                && Vector3.Distance(a.size, b.size) <= BoundsTolerance;
        }
    }
}
