using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Forest의 계절 장식 레이어 프리팹(봄·여름·가을·겨울)을 만드는 에디터 도구.
/// 나무와 돌은 채집 오브젝트로 쓰이므로 장식에 넣지 않는다. 가장자리 띠와 숲 섬에는 채집 나무 자리를 남기고 낮은 덮개만 둔다.
/// 봄 레이어가 낮은 식생의 배치 원본이며, 다른 계절은 봄 프리팹의 "그룹/역할" 계층을 읽어 같은 자리를 역할별 프리팹으로 바꾼다.
/// 각 계절은 숲 바닥을 덮는 Cover 그룹을 가진다. 바닥을 작은 칸으로 나눠 무작위로 덮어 기본 숲과 골고루 섞이게 하며, 봄·여름은 팔레트 칸만 바꾼 머티리얼을 입힌 Meadow_Flat 조각, 가을은 마른 황토, 겨울은 눈 지형 조각을 쓴다.
/// 외곽 암벽과 보스 고지는 네 계절 모두 봄(기본) 그대로 두며, 계절 덮개는 숲 바닥의 약 70%만 덮어 기본 숲이 약 30% 보이게 한다.
/// 열린 씬의 World_PurePoly에서 위치를 읽고, 미리보기 씬에서 조립하므로 열린 씬은 수정하지 않는다.
/// </summary>
public static class SeasonalForestLayerBuilder
{
    private const string c_packRoot = "Assets/Imports/SJT/Pure Poly/Ultimate Low Poly Nature Pack/Prefabs";
    private const string c_outputFolder = "Assets/SJT/03_Prefabs/Map(New)/Seasonal";
    private const string c_worldName = "World_PurePoly";
    private const string c_logPrefix = "[SeasonalForestLayerBuilder] ";
    private const int c_springSeed = 20261120;
    private const float c_gatherClearance = 1.5f;
    private const float c_monsterClearance = 3f;
    private const float c_gateClearance = 10f;
    private const float c_trailHalfWidth = 5f;
    private const float c_ringRadius = 40f;
    private const float c_edgeInner = 80f;
    private const float c_edgeOuter = 98f;
    private const float c_fieldRadius = 84f;
    private const int c_fieldPatchCount = 170;
    private const int c_edgePatchCount = 70;

    // 바닥 덮개 타일: 납작하게 눌러 기존 바닥 충돌 높이(약 0.72m) 근처에 둔다
    private const float c_coverCenterY = 0.65f;
    private const float c_coverHeightScale = 0.35f;
    // 덮개 한 칸의 대략적인 크기(m). 바닥 타일(약 45m)을 이 크기로 나눠 칸마다 덮을지 정한다
    private const float c_coverCellSize = 22.5f;
    // 계절별(봄·여름·가을·겨울) 바닥 칸을 덮을 확률. 숲 바닥 렌더 변화 약 70%(기본 숲 약 30%)가 되도록 렌더 측정으로 맞춘다
    private static readonly float[] coverChance = { 0.45f, 0.42f, 0.52f, 0.44f };

    // 봄·여름 바닥 팔레트: Meadow_Flat이 쓰는 팔레트 칸(2행 7열, 셀 중심 약 #6A9634의 그라데이션)만 계절 단색으로 바꾼 머티리얼을 봄·여름 덮개 조각에 입힌다
    private const string c_dataFolder = "Assets/SJT/04_Data/Season";
    private const string c_sourcePalette = "Assets/Imports/SJT/Pure Poly/Ultimate Low Poly Nature Pack/Textures/PP_Color_Palette.png";
    private const string c_sourceMaterial = "Assets/Imports/SJT/Pure Poly/Ultimate Low Poly Nature Pack/Materials/PP_Standard_Material.mat";
    private const int c_meadowCellRow = 1;
    private const int c_meadowCellColumn = 6;
    private static readonly Color springMeadowColor = new Color32(0xCC, 0xE2, 0x7A, 0xFF);
    private static readonly Color summerMeadowColor = new Color32(0x3D, 0x67, 0x15, 0xFF);

    // 돌문·이음새 입구 (Forest 기준 실측 좌표). 장식을 두지 않는다
    private static readonly Vector2[] gatePoints = { new Vector2(0f, 100f), new Vector2(98f, 40f), new Vector2(-98f, 40f), new Vector2(0f, -100f) };

    // 길 구간 (남쪽 입구 → 고지 계단, 고지 → 북·동·서 돌문). 고지 둘레 순환길은 반지름 c_ringRadius
    private static readonly Vector2[,] trailSegments = { { new Vector2(0f, -122f), new Vector2(0f, -50f) }, { new Vector2(0f, 40f), new Vector2(0f, 132f) }, { new Vector2(36f, 17f), new Vector2(135f, 43f) }, { new Vector2(-36f, 17f), new Vector2(-135f, 43f) } };

    // 숲 섬 (채집 나무 자리로 비워 두는 빈터 실측): x, z, 반지름
    private static readonly Vector3[] groves = { new Vector3(-24f, 36f, 14f), new Vector3(24f, 32f, 14f), new Vector3(24f, 88f, 11f), new Vector3(68f, -4f, 14f), new Vector3(44f, -64f, 13f), new Vector3(-84f, -32f, 10f), new Vector3(-56f, 40f, 10f) };

    private static readonly string[] seasonNames = { "Spring", "Summer", "Autumn", "Winter" };

    /// <summary>
    /// 같은 종류로 취급하는 프리팹 묶음과 배치 크기.
    /// </summary>
    private sealed class Family
    {
        public string key;
        public string[] names;
        public float min;
        public float max;
        public bool useWidth;
        public bool castShadow;
        public float keep = 1f;
        public float sink;
        public List<GameObject> prefabs;
    }

    /// <summary>
    /// 기본 지형 하나(덮을 대상)의 위치·크기 정보.
    /// </summary>
    private struct TerrainPart
    {
        public Bounds bounds;
    }

    /// <summary>
    /// 미리보기 씬에서 지면 높이를 조회하고 월드의 배치 제약을 담는 작업 공간.
    /// </summary>
    private sealed class Workspace
    {
        public Scene scene;
        public PhysicsScene physics;
        public List<Vector2> gathers = new List<Vector2>();
        public List<Vector2> monsters = new List<Vector2>();
        public List<TerrainPart> groundTiles = new List<TerrainPart>();

        public List<Bounds> ponds = new List<Bounds>();
        public Bounds plateau;
        public Bounds stair;
        public HashSet<Collider> mountainColliders = new HashSet<Collider>();
        public HashSet<Collider> plateauColliders = new HashSet<Collider>();
    }

    private static readonly Dictionary<string, Family> families = new Dictionary<string, Family>();

    [MenuItem("Tools/Season/Forest 봄 레이어 생성")]
    private static void BuildSpringMenu()
    {
        Debug.Log(BuildSpring());
    }

    [MenuItem("Tools/Season/Forest 여름·가을·겨울 레이어 생성")]
    private static void BuildOtherSeasonsMenu()
    {
        Debug.Log(BuildOtherSeasons());
    }

    [MenuItem("Tools/Season/Forest 계절 레이어 검사")]
    private static void ValidateMenu()
    {
        Debug.Log(ValidateAll());
    }

    /// <summary>
    /// 봄 레이어(낮은 식생 원본)를 규칙에 따라 배치하고 Forest_Spring 프리팹으로 저장한다.
    /// </summary>
    public static string BuildSpring()
    {
        DefineFamilies();
        Workspace ws = OpenWorkspace();
        if (ws == null)
        {
            return c_logPrefix + "열린 씬에서 World_PurePoly의 Forest 구성(바닥·암벽·고지·마커)을 찾지 못했습니다";
        }

        try
        {
            var rnd = new System.Random(c_springSeed);
            var root = new GameObject("Forest_Spring");
            SceneManager.MoveGameObjectToScene(root, ws.scene);
            AddCover(ws, new System.Random(c_springSeed + 1), root.transform, 0);

            // 가장자리 띠·숲 섬: 채집 나무 자리를 남기고 낮은 풀·민들레 덮개만 둔다
            Transform edge = Group(root.transform, "Edge");
            var edgeRoles = new[] { ("Grass", "grass", 3f), ("Dandelion", "dandelion", 2f), ("Daffodil", "daffodil", 2f) };
            var edgeCenters = new List<Vector2>();
            for (int i = 0; i < 3000 && edgeCenters.Count < c_edgePatchCount; i++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2f);
                float r = c_edgeInner + (float)rnd.NextDouble() * (c_edgeOuter - c_edgeInner);
                var c = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                if (InGateSector(c) || !IsClear(ws, c, 1f) || TooClose(edgeCenters, c, 6f))
                {
                    continue;
                }
                edgeCenters.Add(c);
                PlacePatch(ws, rnd, edge, Pick(rnd, edgeRoles), c, 5, 2.5f);
            }
            foreach (Vector3 g in groves)
            {
                int count = Mathf.RoundToInt(g.z * g.z / 40f);
                for (int i = 0; i < count; i++)
                {
                    float a = (float)(rnd.NextDouble() * Mathf.PI * 2f);
                    float d = Mathf.Sqrt((float)rnd.NextDouble()) * g.z;
                    var c = new Vector2(g.x + Mathf.Cos(a) * d, g.y + Mathf.Sin(a) * d);
                    if (IsClear(ws, c, 1f))
                    {
                        PlacePatch(ws, rnd, edge, Pick(rnd, edgeRoles), c, 5, 2.5f);
                    }
                }
            }

            // 들판 꽃·풀 무리 (반지름 3~5m의 큰 무리)
            Transform field = Group(root.transform, "Field");
            var patchRoles = new[] { ("Daffodil", "daffodil", 3f), ("Hyacinth", "hyacinth", 2f), ("Pansy", "pansy", 2f), ("Dandelion", "dandelion", 1.5f), ("ArumLily", "arumLily", 1f), ("Grass", "grass", 3f), ("BushBerries", "bushBerries", 1f) };
            var patchCenters = new List<Vector2>();
            for (int i = 0; i < 3000 && patchCenters.Count < c_fieldPatchCount; i++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2f);
                float d = Mathf.Sqrt((float)rnd.NextDouble()) * c_fieldRadius;
                var c = new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
                if (!IsFieldSpot(ws, c) || TooClose(patchCenters, c, 7f))
                {
                    continue;
                }
                patchCenters.Add(c);
                (string group, string fam, float w) role = Pick(rnd, patchRoles);
                bool isBush = role.fam == "bushBerries";
                PlacePatch(ws, rnd, field, role, c, isBush ? 2 + rnd.Next(2) : 9 + rnd.Next(4), isBush ? 2f : 3f + (float)rnd.NextDouble() * 2f);
            }

            // 연못: 가장자리 갈대, 수면 연꽃
            Transform pond = Group(root.transform, "Pond");
            foreach (Bounds pb in ws.ponds)
            {
                for (int i = 0; i < 10; i++)
                {
                    float a = (float)(rnd.NextDouble() * Mathf.PI * 2f);
                    float k = 1.0f + (float)rnd.NextDouble() * 0.15f;
                    var p = new Vector2(pb.center.x + Mathf.Cos(a) * pb.extents.x * k, pb.center.z + Mathf.Sin(a) * pb.extents.z * k);
                    if (IsClear(ws, p, 0.5f))
                    {
                        Place(ws, rnd, Group(pond, "Reed"), "reed", p, -1f);
                    }
                }
                for (int i = 0; i < 8; i++)
                {
                    float a = (float)(rnd.NextDouble() * Mathf.PI * 2f);
                    float k = Mathf.Sqrt((float)rnd.NextDouble()) * 0.7f;
                    var p = new Vector2(pb.center.x + Mathf.Cos(a) * pb.extents.x * k, pb.center.z + Mathf.Sin(a) * pb.extents.z * k);
                    if (IsClear(ws, p, 0.5f))
                    {
                        Place(ws, rnd, Group(pond, "WaterLily"), "waterLily", p, pb.max.y - 0.02f);
                    }
                }
            }

            // 보스 고지 발치 덤불 (남쪽 계단 동선 제외)
            PlaceAroundPlateau(ws, rnd, Group(Group(root.transform, "Rim"), "RimBush"), "bush", 24, 1.5f, 3.5f);

            // 길 징검돌
            Transform trail = Group(Group(root.transform, "Trail"), "StonePath");
            foreach (Vector2 p in TrailPoints(rnd, 4f, 1f))
            {
                if (IsClear(ws, p, 0.5f))
                {
                    Place(ws, rnd, trail, "stonePath", p, -1f);
                }
            }

            return Save(root, "Spring");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(ws.scene);
        }
    }

    /// <summary>
    /// 저장된 Forest_Spring 프리팹을 읽어 여름·가을·겨울 프리팹을 생성한다.
    /// 각 계절은 지형 덮개(Cover)를 먼저 깔고, 봄 자리의 식생을 덮개 위 높이로 다시 배치한다.
    /// </summary>
    public static string BuildOtherSeasons()
    {
        DefineFamilies();
        var spring = AssetDatabase.LoadAssetAtPath<GameObject>(c_outputFolder + "/Forest_Spring.prefab");
        if (spring == null)
        {
            return c_logPrefix + "Forest_Spring 프리팹이 없습니다. 봄 레이어를 먼저 생성하세요";
        }

        var report = new StringBuilder();
        for (int s = 1; s < 4; s++)
        {
            Workspace ws = OpenWorkspace();
            if (ws == null)
            {
                return c_logPrefix + "열린 씬에서 World_PurePoly의 Forest 구성(바닥·암벽·고지·마커)을 찾지 못했습니다";
            }

            try
            {
                var rnd = new System.Random(c_springSeed + s * 7919);
                string season = seasonNames[s];
                var root = new GameObject("Forest_" + season);
                SceneManager.MoveGameObjectToScene(root, ws.scene);

                AddCover(ws, rnd, root.transform, s);
                Physics.SyncTransforms();

                Dictionary<string, string> map = RoleMap(s);
                foreach (Transform group in spring.transform)
                {
                    foreach (Transform roleGroup in group)
                    {
                        string role = group.name + "/" + roleGroup.name;
                        if (!map.TryGetValue(role, out string target))
                        {
                            continue;
                        }
                        string srcFamily = SpringFamilyOf(role);
                        Transform dst = Group(Group(root.transform, group.name), roleGroup.name);
                        foreach (Transform src in roleGroup)
                        {
                            Family tf = families[target];
                            if (tf.keep < 1f && rnd.NextDouble() > tf.keep)
                            {
                                continue;
                            }
                            var p = new Vector2(src.position.x, src.position.z);
                            // 덮개가 깔렸으므로 높이를 다시 구한다. 수면 위 연꽃처럼 지면에 닿지 않으면 봄 높이를 쓴다
                            float y = GroundHeight(ws, p, true, out float hitY) ? hitY : src.position.y + families[srcFamily].sink;
                            if (role == "Pond/WaterLily" && s != 3)
                            {
                                y = src.position.y + families[srcFamily].sink;
                            }
                            var pos = new Vector3(p.x, y, p.y);
                            float t = NormalizedSize(families[srcFamily], src);
                            if (target == srcFamily)
                            {
                                SpawnSameVariant(src, dst, pos, families[target]);
                            }
                            else
                            {
                                Spawn(rnd, dst, target, pos, src.eulerAngles.y, t);
                            }
                        }
                    }
                }

                AddSeasonExtras(ws, rnd, root.transform, spring.transform, s);
                report.AppendLine(Save(root, season));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(ws.scene);
            }
        }
        return report.ToString();
    }

    /// <summary>
    /// 저장된 네 계절 프리팹의 Collider·Static·마커 여백·개수를 검사한 결과를 반환한다.
    /// 지형 덮개(Cover)와 FX는 마커 여백 검사에서 제외한다(납작한 바닥이거나 공중 효과).
    /// </summary>
    public static string ValidateAll()
    {
        GameObject world = GameObject.Find(c_worldName);
        var gathers = new List<Vector2>();
        var monsters = new List<Vector2>();
        if (world == null || !CollectMarkers(world.transform, gathers, monsters))
        {
            return c_logPrefix + "열린 씬에서 World_PurePoly 마커를 찾지 못해 검사할 수 없습니다";
        }

        var sb = new StringBuilder(c_logPrefix + "계절 레이어 검사\n");
        foreach (string season in seasonNames)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(c_outputFolder + "/Forest_" + season + ".prefab");
            if (prefab == null)
            {
                sb.AppendLine(season + ": 없음");
                continue;
            }
            int colliders = prefab.GetComponentsInChildren<Collider>(true).Length;
            int particles = prefab.GetComponentsInChildren<ParticleSystem>(true).Length;
            int statics = 0;
            int violations = 0;
            int instances = 0;
            int cover = 0;
            var perGroup = new SortedDictionary<string, int>();
            foreach (Transform group in prefab.transform)
            {
                foreach (Transform roleGroup in group)
                {
                    perGroup[group.name + "/" + roleGroup.name] = roleGroup.childCount;
                    foreach (Transform item in roleGroup)
                    {
                        instances++;
                        if (GameObjectUtility.GetStaticEditorFlags(item.gameObject) != 0)
                        {
                            statics++;
                        }
                        if (group.name == "Cover")
                        {
                            cover++;
                            continue;
                        }
                        if (group.name == "FX")
                        {
                            continue;
                        }
                        var p = new Vector2(item.position.x, item.position.z);
                        if (MinDistance(gathers, p) < c_gatherClearance - 0.01f || MinDistance(monsters, p) < c_monsterClearance - 0.01f)
                        {
                            violations++;
                        }
                    }
                }
            }
            sb.AppendLine($"{season}: 배치 {instances} (지형 덮개 {cover}) · 파티클 {particles} · Collider {colliders} · Static {statics} · 마커 여백 위반 {violations}");
            foreach (KeyValuePair<string, int> kv in perGroup)
            {
                sb.AppendLine($"    {kv.Key} {kv.Value}");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 계절별 역할 → 프리팹 묶음 매핑. 값이 봄과 같으면 같은 프리팹을 유지한다.
    /// </summary>
    private static Dictionary<string, string> RoleMap(int season)
    {
        var map = new Dictionary<string, string>
        {
            { "Edge/Grass", "grass" }, { "Edge/Dandelion", "dandelion" }, { "Edge/Daffodil", "daffodil" },
            { "Field/Daffodil", "daffodil" }, { "Field/Hyacinth", "hyacinth" }, { "Field/Pansy", "pansy" }, { "Field/Dandelion", "dandelion" }, { "Field/ArumLily", "arumLily" }, { "Field/Grass", "grass" }, { "Field/BushBerries", "bushBerries" },
            { "Pond/Reed", "reed" }, { "Pond/WaterLily", "waterLily" }, { "Rim/RimBush", "bush" }, { "Trail/StonePath", "stonePath" }
        };
        if (season == 1)
        {
            map["Edge/Grass"] = "cemeteryGrass";
            map["Edge/Dandelion"] = "grassTall";
            map["Edge/Daffodil"] = "sunflower";
            map["Field/Daffodil"] = "sunflower";
            map["Field/Hyacinth"] = "plant";
            map["Field/Pansy"] = "daisy";
            map["Field/Dandelion"] = "cemeteryGrass";
            map["Field/ArumLily"] = "rose";
            map["Field/Grass"] = "cemeteryGrass";
            map["Field/BushBerries"] = "bush";
        }
        else if (season == 2)
        {
            map["Edge/Grass"] = "desertGrass";
            map["Edge/Dandelion"] = "leafCarpet";
            map["Edge/Daffodil"] = "pileLeaves";
            map["Field/Daffodil"] = "pileLeaves";
            map["Field/Hyacinth"] = "mushroomBrown";
            map["Field/Pansy"] = "leafCarpet";
            map["Field/Dandelion"] = "dandelionSeed";
            map["Field/ArumLily"] = "flyAgaric";
            map["Field/Grass"] = "desertGrass";
            map["Pond/WaterLily"] = "leafWater";
        }
        else if (season == 3)
        {
            map["Edge/Grass"] = "grassSnow";
            map["Edge/Dandelion"] = "pebblesSnow";
            map["Edge/Daffodil"] = "grassSnow";
            map["Field/Daffodil"] = "grassSnow";
            map["Field/Hyacinth"] = "bushSnow";
            map["Field/Pansy"] = "stoneSnow";
            map["Field/Dandelion"] = "pebblesSnow";
            map["Field/ArumLily"] = "grassSnow";
            map["Field/Grass"] = "grassSnow";
            map["Field/BushBerries"] = "bushSnow";
            map["Pond/Reed"] = "bushSnow";
            map["Pond/WaterLily"] = "iceFloe";
            map["Trail/StonePath"] = "floorTileSnow";
        }
        return map;
    }

    private static string SpringFamilyOf(string role)
    {
        return RoleMap(0)[role];
    }

    /// <summary>
    /// 숲 바닥을 계절 덮개 조각으로 덮는다. 바닥 타일을 약 22.5m 칸(c_coverCellSize)으로 나누고 칸마다 무작위로 골라(계절별 확률),
    /// 위치·크기·각도를 흔들어 놓는다. 덮인 곳과 기본 숲이 덩어리지지 않고 골고루 섞여 보이게 하기 위해서다.
    /// 봄·여름은 Meadow_Flat 조각에 계절 팔레트 머티리얼을 입히고, 가을은 마른 황토, 겨울은 눈 지형 조각을 쓴다.
    /// 덮개 메시는 미리보기 씬에 충돌 복제본을 만들어 이후 배치의 지면이 되게 한다.
    /// </summary>
    private static void AddCover(Workspace ws, System.Random rnd, Transform root, int season)
    {
        Transform ground = Group(Group(root, "Cover"), "Ground");
        string fam = season == 3 ? "arcticGroundCover" : season == 2 ? "dryGroundCover" : "meadowPaletteCover";
        Material palette = season <= 1 ? EnsurePaletteMaterial(season) : null;
        if (season <= 1 && palette == null)
        {
            return;
        }

        foreach (TerrainPart tile in ws.groundTiles)
        {
            int nx = Mathf.Max(1, Mathf.RoundToInt(tile.bounds.size.x / c_coverCellSize));
            int nz = Mathf.Max(1, Mathf.RoundToInt(tile.bounds.size.z / c_coverCellSize));
            float cellX = tile.bounds.size.x / nx;
            float cellZ = tile.bounds.size.z / nz;
            float h = tile.bounds.size.y * c_coverHeightScale;
            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    if (rnd.NextDouble() > coverChance[season])
                    {
                        continue;
                    }
                    var c = new Vector3(tile.bounds.min.x + cellX * (ix + 0.5f + (float)(rnd.NextDouble() - 0.5) * 0.4f), c_coverCenterY, tile.bounds.min.z + cellZ * (iz + 0.5f + (float)(rnd.NextDouble() - 0.5) * 0.4f));
                    float size = 0.85f + (float)rnd.NextDouble() * 0.3f;
                    var target = new Bounds(c, new Vector3(cellX * size, h, cellZ * size));
                    GameObject go = SpawnFitted(rnd, ground, fam, target, (float)rnd.NextDouble() * 360f);
                    if (go == null)
                    {
                        continue;
                    }
                    if (palette != null)
                    {
                        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                        {
                            var mats = new Material[r.sharedMaterials.Length];
                            for (int i = 0; i < mats.Length; i++)
                            {
                                mats[i] = palette;
                            }
                            r.sharedMaterials = mats;
                        }
                    }
                    CloneCollider(ws, go.transform, null);
                }
            }
        }
    }

    /// <summary>
    /// 봄·여름 팔레트 텍스처와 머티리얼을 만들거나 기존 것을 반환한다.
    /// 원본 팔레트를 복사(임포트 설정 유지)한 뒤 Meadow_Flat이 쓰는 칸만 계절색으로 칠한다. 텍스처 색은 호출할 때마다 현재 상수로 다시 칠한다.
    /// </summary>
    private static Material EnsurePaletteMaterial(int season)
    {
        string name = season == 0 ? "Spring" : "Summer";
        string texPath = c_dataFolder + "/PP_Color_Palette_" + name + ".png";
        string matPath = c_dataFolder + "/PP_Season_" + name + "_Material.mat";
        if (!AssetDatabase.IsValidFolder(c_dataFolder))
        {
            AssetDatabase.CreateFolder("Assets/SJT/04_Data", "Season");
        }
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null && !AssetDatabase.CopyAsset(c_sourcePalette, texPath))
        {
            Debug.LogWarning(c_logPrefix + "팔레트 텍스처를 복사하지 못했습니다: " + texPath);
            return null;
        }
        var work = new Texture2D(2, 2);
        work.LoadImage(System.IO.File.ReadAllBytes(c_sourcePalette));
        int cell = work.width / 8;
        Color color = season == 0 ? springMeadowColor : summerMeadowColor;
        int x0 = c_meadowCellColumn * cell;
        int y0 = work.height - (c_meadowCellRow + 1) * cell;
        for (int y = y0; y < y0 + cell; y++)
        {
            for (int x = x0; x < x0 + cell; x++)
            {
                work.SetPixel(x, y, color);
            }
        }
        work.Apply();
        System.IO.File.WriteAllBytes(texPath, work.EncodeToPNG());
        Object.DestroyImmediate(work);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

        // 머티리얼이 이미 있으면 텍스처 색만 갱신하고 그대로 쓴다
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null)
        {
            return existing;
        }
        var source = AssetDatabase.LoadAssetAtPath<Material>(c_sourceMaterial);
        var mat = new Material(source) { name = "PP_Season_" + name + "_Material" };
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tex);
        }
        AssetDatabase.CreateAsset(mat, matPath);
        return AssetDatabase.LoadAssetAtPath<Material>(matPath);
    }

    /// <summary>
    /// 계절 전용 추가물을 배치한다. 나무·돌은 쓰지 않는다.
    /// </summary>
    private static void AddSeasonExtras(Workspace ws, System.Random rnd, Transform root, Transform spring, int season)
    {
        Transform extra = Group(root, "Extra");
        Transform fx = Group(root, "FX");
        List<Vector2> patchCenters = FlowerPatchCenters(spring);

        if (season == 1)
        {
            PlaceFx(ws, fx, "fxButterfliesColorful", Take(patchCenters, rnd, 6), 1.5f);
            PlaceFx(ws, fx, "fxFlies", PondCenters(ws), 1f);
        }
        else if (season == 2)
        {
            Transform leaf = Group(extra, "LeafStrip");
            foreach (Vector2 c in TrailPoints(rnd, 6f, 0.5f))
            {
                for (int i = 0; i < 5; i++)
                {
                    var p = new Vector2(c.x + (float)(rnd.NextDouble() - 0.5) * 4f, c.y + (float)(rnd.NextDouble() - 0.5) * 4f);
                    if (IsClear(ws, p, 0f))
                    {
                        Place(ws, rnd, leaf, "leafCarpet", p, -1f, true);
                    }
                }
            }
            Transform pumpkin = Group(extra, "Pumpkin");
            foreach (Vector2 gp in gatePoints)
            {
                // 돌문 여백(10m) 바로 바깥, 입구 옆에 둔다
                Vector2 side = new Vector2(-gp.y, gp.x).normalized * 6f;
                Vector2 inward = -gp.normalized * 10f;
                for (int i = 0; i < 3; i++)
                {
                    var p = gp + inward + side + new Vector2(i * 1.2f, i * 0.8f);
                    if (IsClear(ws, p, 0.5f))
                    {
                        Place(ws, rnd, pumpkin, "pumpkin", p, -1f, true);
                    }
                }
            }
            // Deadfall은 길이 6~9m, Tree_Trunk는 약 4m라 외형 반경만큼 마커 여백을 더 둔다
            PlaceInField(ws, rnd, Group(extra, "Deadfall"), "deadfall", 5, 4.5f, patchCenters);
            PlaceInField(ws, rnd, Group(extra, "TreeTrunk"), "treeTrunk", 5, 2.5f, patchCenters);
            var groveTops = new List<Vector2>();
            foreach (Vector3 g in groves)
            {
                groveTops.Add(new Vector2(g.x, g.y));
            }
            PlaceFx(ws, fx, "fxFallingLeaves", Take(groveTops, rnd, 4), 10f);
            PlaceFx(ws, fx, "fxFallingLeavesColorful", new List<Vector2> { new Vector2(-80f, 40f), new Vector2(80f, 30f), new Vector2(0f, -78f) }, 10f);
        }
        else if (season == 3)
        {
            PlaceInField(ws, rnd, Group(extra, "TreeStumpSnow"), "treeStumpSnow", 8, 1.5f, patchCenters);
            PlaceInField(ws, rnd, Group(extra, "TreeTrunkSnow"), "treeTrunkSnow", 5, 2.5f, patchCenters);
            PlaceFx(ws, fx, "fxSnowSwirl", new List<Vector2> { new Vector2(-50f, -10f), new Vector2(50f, -45f), new Vector2(-10f, 75f) }, 3f);
            PlaceFx(ws, fx, "fxFogWhite", PondCenters(ws), 0.5f);
        }
    }

    /// <summary>
    /// 프리팹 묶음과 크기 범위(미터)를 정의한다. 크기 기준은 BaseCamp에 실제 배치된 풀·꽃이다.
    /// Cover 계열은 SpawnFitted가 대상 bounds에 맞추므로 크기 범위를 쓰지 않는다.
    /// </summary>
    private static void DefineFamilies()
    {
        families.Clear();
        Add("bush", "Forest/Plants", new[] { "Bush" }, 1.6f, 2.4f, false, true);
        Add("daffodil", "Forest/Flowers", new[] { "Daffodil" }, 1.3f, 1.8f, false, false);
        Add("hyacinth", "Forest/Flowers", new[] { "Hyacinth" }, 1.2f, 1.6f, false, false);
        Add("pansy", "Forest/Flowers", new[] { "Pansy_Flower_Pink", "Pansy_Flower_Purple" }, 0.9f, 1.2f, false, false);
        Add("dandelion", "Forest/Flowers", new[] { "Dandelion" }, 0.9f, 1.2f, false, false);
        Add("arumLily", "Forest/Flowers", new[] { "Arum_Lily_White" }, 1.4f, 1.8f, false, false);
        Add("grass", "Forest/Plants", new[] { "Grass", "Grass_Single" }, 1f, 1.4f, false, false);
        Add("bushBerries", "Forest/Plants", new[] { "Bush_Berries" }, 1.4f, 2f, false, true);
        Add("reed", "Beach/Water Plants", new[] { "Reed" }, 1.6f, 2.4f, false, false);
        Add("waterLily", "Flying Islands/Flowers", new[] { "Water_Lily", "Water_Lily_Leaf" }, 0.8f, 1.2f, true, false);
        Add("stonePath", "Forest/Rocks & Stones", new[] { "Stone_Path" }, 1.6f, 2.4f, true, false, 0.05f);

        Add("sunflower", "Forest/Flowers", new[] { "Sunflower" }, 2.6f, 3.4f, false, false);
        Add("plant", "Forest/Plants", new[] { "Plant" }, 2.2f, 3.2f, false, true);
        Add("daisy", "Flying Islands/Flowers", new[] { "Daisy_Flower_Blue", "Daisy_Flower_Orange", "Daisy_Flower_Purple" }, 1f, 1.4f, false, false);
        Add("grassTall", "Forest/Plants", new[] { "Grass" }, 1.4f, 1.9f, false, false);
        Add("cemeteryGrass", "Cemetery/Plants", new[] { "Cemetery_Grass" }, 1.5f, 2.1f, false, false);
        Add("rose", "Flying Islands/Flowers", new[] { "Rose" }, 1.3f, 1.7f, false, false);

        Add("pileLeaves", "Cemetery/Trees", new[] { "Pile_Leaves" }, 2.2f, 3.2f, true, false, 0.05f).keep = 0.5f;
        Add("mushroomBrown", "Cemetery/Mushrooms", new[] { "Mushroom_Brown", "Mushroom_Chanterelle" }, 0.5f, 0.9f, false, false);
        Add("desertGrass", "Desert/Plants", new[] { "Desert_Grass" }, 0.9f, 1.3f, false, false);
        Add("dandelionSeed", "Forest/Flowers", new[] { "Dandelion_Seed_Head" }, 0.6f, 0.9f, false, false);
        Add("flyAgaric", "Cemetery/Mushrooms", new[] { "Mushroom_Fly_Agaric" }, 0.6f, 1f, false, false);
        Add("leafWater", "Cemetery/Trees", new[] { "Leaf_02", "Leaf_05" }, 1f, 1.5f, true, false);
        Add("leafCarpet", "Cemetery/Trees", new[] { "Leaf_02", "Leaf_04", "Leaf_05" }, 1.5f, 2.5f, true, false);
        Add("pumpkin", "Cemetery/Pumpkins", new[] { "Pumpkin" }, 0.8f, 1.3f, true, true);
        Add("deadfall", "Forest/Trees", new[] { "Deadfall" }, 6f, 9f, true, true, 0.2f);
        Add("treeTrunk", "Forest/Trees", new[] { "Tree_Trunk" }, 3.5f, 5f, true, true, 0.1f);

        Add("bushSnow", "Arctic/Plants", new[] { "Bush_Snow" }, 1.4f, 2.2f, false, true);
        Add("grassSnow", "Arctic/Plants", new[] { "Grass_Snow" }, 0.9f, 1.2f, false, false);
        Add("stoneSnow", "Arctic/Rocks", new[] { "Stone_Snow" }, 0.8f, 1.4f, true, false, 0.1f);
        Add("pebblesSnow", "Arctic/Rocks", new[] { "Pebbles_Snow" }, 1.2f, 2f, true, false, 0.05f);
        Add("iceFloe", "Arctic/Ice Props", new[] { "Ice_Floe" }, 1.5f, 2.5f, true, false);
        Add("floorTileSnow", "Arctic/Rocks", new[] { "Floor_Tile_Snow" }, 1.6f, 2.4f, true, false, 0.05f);
        Add("treeStumpSnow", "Arctic/Trees", new[] { "Tree_Stump_Snow" }, 1.2f, 1.8f, false, true, 0.1f);
        Add("treeTrunkSnow", "Arctic/Trees", new[] { "Tree_Trunk_Snow" }, 3.5f, 5f, true, true, 0.1f);

        // 지형 덮개 (SpawnFitted로 bounds에 맞춤). 묶음마다 정한 높이 이하의 납작한 변형만 쓴다
        // 납작하게 눌러 까는 바닥 덮개는 자기 그림자로 어둡게 보이므로 그림자를 끈다
        AddCover("arcticGroundCover", "Arctic/Terrain", new[] { "Arctic_Ground" }, 2.2f, false);
        AddCover("dryGroundCover", "Desert/Terrain", new[] { "Desert_Ground_Stones" }, 1.6f, false);
        AddCover("meadowPaletteCover", "Forest/Terrain", new[] { "Meadow_Flat" }, 2.0f, false);

        Add("fxButterfliesColorful", "FX/Butterflies", new[] { "FX_Butterflies_Colorful_01" }, 1f, 1f, false, false);
        Add("fxFlies", "FX/Flies", new[] { "FX_Flies_01" }, 1f, 1f, false, false);
        Add("fxFallingLeaves", "FX/Wind", new[] { "FX_Falling_Leaves_Orange_Purple_01" }, 1f, 1f, false, false);
        Add("fxFallingLeavesColorful", "FX/Wind", new[] { "FX_Falling_Leaves_Colorful_01" }, 1f, 1f, false, false);
        Add("fxSnowSwirl", "FX/Snow", new[] { "FX_Snow_Swirl_01" }, 1f, 1f, false, false);
        Add("fxFogWhite", "FX/Fog", new[] { "FX_Fog_White_01" }, 1f, 1f, false, false);
    }

    /// <summary>
    /// 지형 덮개 묶음을 정의한다. maxHeight보다 높은 변형(바닥이 울퉁불퉁해짐)과 자식 Renderer가 붙은 변형은 제외한다.
    /// </summary>
    private static void AddCover(string key, string folder, string[] names, float maxHeight, bool castShadow)
    {
        Family f = Add(key, folder, names, 0f, 0f, false, castShadow);
        // 높이 제한을 넘거나, 자식 Renderer(예: Arctic_Ground_10의 수면 메시)가 붙은 변형은 제외한다. 자식 수면이 눈 위에 남색 판으로 덮이기 때문이다
        f.prefabs.RemoveAll(p => MeshSize(p, false) > maxHeight || p.GetComponentsInChildren<Renderer>(true).Length > 1);
        if (f.prefabs.Count == 0)
        {
            Debug.LogWarning(c_logPrefix + $"{key}: 높이 {maxHeight}m 이하인 변형이 없습니다");
        }
    }

    private static Family Add(string key, string folder, string[] names, float min, float max, bool useWidth, bool castShadow, float sink = 0f)
    {
        var f = new Family { key = key, names = names, min = min, max = max, useWidth = useWidth, castShadow = castShadow, sink = sink, prefabs = new List<GameObject>() };
        string dir = c_packRoot + "/" + folder;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { dir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            foreach (string n in names)
            {
                bool exact = n.StartsWith("FX_") || Regex.IsMatch(n, @"_\d+$");
                bool match = exact ? (file == n || file == "PP_" + n) : Regex.IsMatch(file, "^PP_" + Regex.Escape(n) + @"_\d+$");
                if (match)
                {
                    f.prefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    break;
                }
            }
        }
        f.prefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (f.prefabs.Count == 0)
        {
            Debug.LogWarning(c_logPrefix + $"{key} 프리팹을 찾지 못했습니다: {dir}");
        }
        families[key] = f;
        return f;
    }

    /// <summary>
    /// 열린 씬의 World_PurePoly에서 배치 제약과 덮을 지형을 읽고, 미리보기 씬에 지면·고지·암벽 충돌 복제본을 만든다.
    /// 필요한 계층이 없으면 미리보기 씬을 만들지 않고 null을 반환한다.
    /// </summary>
    private static Workspace OpenWorkspace()
    {
        GameObject world = GameObject.Find(c_worldName);
        if (world == null)
        {
            return null;
        }
        Transform forest = world.transform.Find("Forest");
        Transform ground = forest != null ? forest.Find("Map/00_Ground") : null;
        Transform boundary = forest != null ? forest.Find("Map/04_ForestBoundary") : null;
        Transform plateau = forest != null ? forest.Find("Map/07_FinalBossPlateau") : null;
        Transform plateauMesh = plateau != null ? plateau.Find("BossPlateau") : null;
        Transform stair = plateau != null ? plateau.Find("Stair") : null;
        if (ground == null || boundary == null || plateauMesh == null || stair == null)
        {
            return null;
        }

        var ws = new Workspace();
        if (!CollectMarkers(world.transform, ws.gathers, ws.monsters))
        {
            return null;
        }
        ws.scene = EditorSceneManager.NewPreviewScene();
        ws.physics = ws.scene.GetPhysicsScene();
        try
        {
            foreach (Transform child in ground)
            {
                var mr = child.GetComponent<MeshRenderer>();
                if (mr == null)
                {
                    continue;
                }
                if (child.name == "PondWater")
                {
                    ws.ponds.Add(mr.bounds);
                    continue;
                }
                ws.groundTiles.Add(new TerrainPart { bounds = mr.bounds });
                CloneCollider(ws, child, null);
            }

            foreach (Transform m in boundary)
            {
                CloneCollider(ws, m, ws.mountainColliders);
            }

            ws.plateau = RendererBounds(plateauMesh);

            ws.stair = RendererBounds(stair);
            foreach (Transform part in plateau)
            {
                CloneCollider(ws, part, ws.plateauColliders);
            }
            Physics.SyncTransforms();
            return ws;
        }
        catch
        {
            EditorSceneManager.ClosePreviewScene(ws.scene);
            throw;
        }
    }

    private static bool CollectMarkers(Transform world, List<Vector2> gathers, List<Vector2> monsters)
    {
        Transform marker = world.Find("Forest/MonsterPlacementMarker");
        Transform gatherRoot = marker != null ? marker.Find("GatherPoints") : null;
        Transform monsterRoot = marker != null ? marker.Find("MonsterPoints") : null;
        if (gatherRoot == null || monsterRoot == null)
        {
            return false;
        }
        foreach (Transform t in gatherRoot)
        {
            gathers.Add(new Vector2(t.position.x, t.position.z));
        }
        foreach (Transform t in monsterRoot)
        {
            monsters.Add(new Vector2(t.position.x, t.position.z));
        }
        Transform boss = marker.Find("FinalBoss");
        if (boss != null)
        {
            monsters.Add(new Vector2(boss.position.x, boss.position.z));
        }
        return true;
    }

    /// <summary>
    /// 메시를 미리보기 씬의 충돌 전용 오브젝트로 복제한다. 저장되는 레이어 계층에는 넣지 않는다.
    /// </summary>
    private static void CloneCollider(Workspace ws, Transform src, HashSet<Collider> registry)
    {
        foreach (MeshFilter mf in src.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null)
            {
                continue;
            }
            var go = new GameObject("Col_" + mf.name);
            SceneManager.MoveGameObjectToScene(go, ws.scene);
            go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation);
            go.transform.localScale = mf.transform.lossyScale;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            registry?.Add(mc);
        }
    }

    private static Bounds RendererBounds(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.zero);
        foreach (Renderer r in rs)
        {
            b.Encapsulate(r.bounds);
        }
        return b;
    }

    /// <summary>
    /// 위에서 아래로 Raycast해 지면(덮개 포함) 높이를 구한다. 암벽에 맞으면 배치하지 않는다.
    /// </summary>
    private static bool GroundHeight(Workspace ws, Vector2 p, bool allowPlateau, out float y)
    {
        y = 0f;
        if (!ws.physics.Raycast(new Vector3(p.x, 120f, p.y), Vector3.down, out RaycastHit hit, 240f))
        {
            return false;
        }
        if (ws.mountainColliders.Contains(hit.collider))
        {
            return false;
        }
        if (!allowPlateau && ws.plateauColliders.Contains(hit.collider))
        {
            return false;
        }
        y = hit.point.y;
        return true;
    }

    /// <summary>
    /// 마커·돌문·계단 여백을 지키는지 확인한다. extra는 장식의 외형 반경만큼 더할 여백이다.
    /// </summary>
    private static bool IsClear(Workspace ws, Vector2 p, float extra, bool avoidTrail = false)
    {
        if (MinDistance(ws.gathers, p) < c_gatherClearance + extra || MinDistance(ws.monsters, p) < c_monsterClearance + extra)
        {
            return false;
        }
        foreach (Vector2 g in gatePoints)
        {
            if (Vector2.Distance(g, p) < c_gateClearance)
            {
                return false;
            }
        }
        if (InStairPath(ws, p))
        {
            return false;
        }
        return !avoidTrail || TrailDistance(p) >= c_trailHalfWidth;
    }

    /// <summary>
    /// 들판 무리를 둘 수 있는 자리인지 확인한다(고지·길·연못·숲 섬·가장자리 띠 제외).
    /// </summary>
    private static bool IsFieldSpot(Workspace ws, Vector2 p)
    {
        if (Vector2.Distance(p, new Vector2(ws.plateau.center.x, ws.plateau.center.z)) < ws.plateau.extents.x + 9f || TrailDistance(p) < c_trailHalfWidth + 1f || p.magnitude > c_edgeInner - 2f)
        {
            return false;
        }
        foreach (Bounds pb in ws.ponds)
        {
            if (Vector2.Distance(p, new Vector2(pb.center.x, pb.center.z)) < pb.extents.x + 5f)
            {
                return false;
            }
        }
        foreach (Vector3 g in groves)
        {
            if (Vector2.Distance(p, new Vector2(g.x, g.y)) < g.z + 3f)
            {
                return false;
            }
        }
        return IsClear(ws, p, 1f, true);
    }

    private static bool InStairPath(Workspace ws, Vector2 p)
    {
        return Mathf.Abs(p.x - ws.stair.center.x) < ws.stair.extents.x + 3f && p.y < ws.stair.max.z + 2f && p.y > ws.stair.min.z - 6f;
    }

    private static bool InGateSector(Vector2 p)
    {
        float deg = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
        foreach (float gd in new[] { 90f, 23f, 157f, -90f })
        {
            if (Mathf.Abs(Mathf.DeltaAngle(deg, gd)) < (gd < 0f ? 14f : 11f))
            {
                return true;
            }
        }
        return false;
    }

    private static float TrailDistance(Vector2 p)
    {
        float d = Mathf.Abs(p.magnitude - c_ringRadius);
        for (int i = 0; i < trailSegments.GetLength(0); i++)
        {
            d = Mathf.Min(d, SegmentDistance(p, trailSegments[i, 0], trailSegments[i, 1]));
        }
        return d;
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static List<Vector2> TrailPoints(System.Random rnd, float step, float jitter)
    {
        var pts = new List<Vector2>();
        for (int i = 0; i < trailSegments.GetLength(0); i++)
        {
            Vector2 a = trailSegments[i, 0];
            Vector2 b = trailSegments[i, 1];
            float len = Vector2.Distance(a, b);
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized;
            for (float d = step * 0.5f; d < len; d += step)
            {
                Vector2 p = Vector2.Lerp(a, b, d / len) + n * (float)(rnd.NextDouble() - 0.5) * 2f * jitter;
                if (Mathf.Abs(p.x) < 128f && p.y < 126f && p.y > -116f)
                {
                    pts.Add(p);
                }
            }
        }
        float ringLen = 2f * Mathf.PI * c_ringRadius;
        for (float d = 0f; d < ringLen; d += step)
        {
            float a = d / c_ringRadius;
            float r = c_ringRadius + (float)(rnd.NextDouble() - 0.5) * 2f * jitter;
            pts.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
        return pts;
    }

    /// <summary>
    /// 중심 주변 반지름 안에 같은 역할의 식생 count개를 무리로 배치한다.
    /// </summary>
    private static void PlacePatch(Workspace ws, System.Random rnd, Transform groupRoot, (string group, string fam, float w) role, Vector2 c, int count, float radius)
    {
        Transform pg = Group(groupRoot, role.group);
        for (int k = 0; k < count; k++)
        {
            float pa = (float)(rnd.NextDouble() * Mathf.PI * 2f);
            float pd = Mathf.Sqrt((float)rnd.NextDouble()) * radius;
            var p = new Vector2(c.x + Mathf.Cos(pa) * pd, c.y + Mathf.Sin(pa) * pd);
            if (IsClear(ws, p, 0f))
            {
                Place(ws, rnd, pg, role.fam, p, -1f);
            }
        }
    }

    /// <summary>
    /// 들판 빈자리(꽃무리와 6m 이상 떨어진 곳)에 큰 장식을 count개 배치한다.
    /// </summary>
    private static void PlaceInField(Workspace ws, System.Random rnd, Transform parent, string family, int count, float footprint, List<Vector2> avoid)
    {
        int placed = 0;
        for (int i = 0; i < 600 && placed < count; i++)
        {
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2f);
            float d = 48f + (float)rnd.NextDouble() * 30f;
            var p = new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
            if (IsFieldSpot(ws, p) && IsClear(ws, p, footprint) && MinDistance(avoid, p) > 6f && Place(ws, rnd, parent, family, p, -1f, true))
            {
                placed++;
            }
        }
    }

    /// <summary>
    /// 보스 고지 발치에 둘러 배치한다. 남쪽 계단 동선과 고지 둘레 순환길은 비운다.
    /// </summary>
    private static void PlaceAroundPlateau(Workspace ws, System.Random rnd, Transform parent, string family, int count, float offsetMin, float offsetMax)
    {
        Vector2 center = new Vector2(ws.plateau.center.x, ws.plateau.center.z);
        float baseR = ws.plateau.extents.x;
        int placed = 0;
        for (int i = 0; i < count * 30 && placed < count; i++)
        {
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2f);
            if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, -90f)) < 16f)
            {
                continue;
            }
            float r = baseR + offsetMin + (float)rnd.NextDouble() * (offsetMax - offsetMin);
            var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            if (!IsClear(ws, p, 0.5f, true))
            {
                continue;
            }
            if (Place(ws, rnd, parent, family, p, -1f))
            {
                placed++;
            }
        }
    }

    private static void PlaceFx(Workspace ws, Transform parent, string family, List<Vector2> points, float height)
    {
        Transform g = Group(parent, family);
        var rnd = new System.Random(c_springSeed + points.Count);
        foreach (Vector2 p in points)
        {
            if (!GroundHeight(ws, p, true, out float y))
            {
                y = 0.7f;
            }
            Spawn(rnd, g, family, new Vector3(p.x, y + height, p.y), 0f, 0f);
        }
    }

    /// <summary>
    /// 지면 높이를 구해 묶음의 무작위 변형을 배치한다. fixedY가 0 이상이면 그 높이(수면 등)를 쓴다.
    /// </summary>
    private static bool Place(Workspace ws, System.Random rnd, Transform parent, string family, Vector2 p, float fixedY, bool allowPlateau = false)
    {
        float y = fixedY;
        if (fixedY < 0f && !GroundHeight(ws, p, allowPlateau, out y))
        {
            return false;
        }
        return Spawn(rnd, parent, family, new Vector3(p.x, y, p.y), (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble()) != null;
    }

    /// <summary>
    /// 중첩 프리팹 인스턴스를 만들고 크기(0~1 비율 t)를 맞춘 뒤 Collider 제거·그림자 설정을 적용한다.
    /// </summary>
    private static GameObject Spawn(System.Random rnd, Transform parent, string family, Vector3 groundPos, float yaw, float t)
    {
        Family f = families[family];
        if (f.prefabs.Count == 0)
        {
            return null;
        }
        GameObject prefab = f.prefabs[rnd.Next(f.prefabs.Count)];
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        float size = MeshSize(prefab, f.useWidth);
        float scale = size > 0.0001f && f.min > 0f && !prefab.name.StartsWith("FX_") ? Mathf.Lerp(f.min, f.max, t) / size : 1f;
        go.transform.SetPositionAndRotation(groundPos - Vector3.up * f.sink, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = prefab.transform.localScale * scale;
        FinishInstance(go, f.castShadow);
        return go;
    }

    /// <summary>
    /// 지형 덮개 변형 하나를 골라 대상 bounds(바닥 기준)에 맞춰 늘린다. 90도 단위 회전이면 bounds에 정확히 맞고, 그 밖의 각도는 회전된 사각형이 된다(바닥 덮개 조각을 흩뿌릴 때 사용).
    /// </summary>
    private static GameObject SpawnFitted(System.Random rnd, Transform parent, string family, Bounds target, float yaw)
    {
        Family f = families[family];
        if (f.prefabs.Count == 0)
        {
            return null;
        }
        GameObject prefab = f.prefabs[rnd.Next(f.prefabs.Count)];
        Bounds mb = MeshBounds(prefab);
        bool swapAxes = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(yaw, 270f)) < 1f;
        float sx = (swapAxes ? target.size.z : target.size.x) / Mathf.Max(mb.size.x, 0.001f);
        float sz = (swapAxes ? target.size.x : target.size.z) / Mathf.Max(mb.size.z, 0.001f);
        float sy = target.size.y / Mathf.Max(mb.size.y, 0.001f);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        var scale = new Vector3(sx, sy, sz);
        Vector3 localCenterBottom = Vector3.Scale(new Vector3(mb.center.x, mb.min.y, mb.center.z), scale);
        Vector3 targetCenterBottom = new Vector3(target.center.x, target.min.y, target.center.z);
        go.transform.SetPositionAndRotation(targetCenterBottom - rot * localCenterBottom, rot);
        go.transform.localScale = Vector3.Scale(prefab.transform.localScale, scale);
        // 지형 덮개는 기본 바닥과 거의 같은 높이라 그림자를 받으면 남색 얼룩(그림자 여드름)이 생긴다
        FinishInstance(go, f.castShadow, false);
        return go;
    }

    /// <summary>
    /// 같은 묶음으로 유지되는 역할은 봄 인스턴스와 같은 변형·크기·회전을 쓰고 높이만 새 지면에 맞춘다.
    /// </summary>
    private static void SpawnSameVariant(Transform src, Transform parent, Vector3 groundPos, Family f)
    {
        GameObject srcPrefab = PrefabUtility.GetCorrespondingObjectFromSource(src.gameObject);
        if (srcPrefab == null)
        {
            return;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(srcPrefab, parent);
        go.transform.SetPositionAndRotation(groundPos - Vector3.up * f.sink, src.rotation);
        go.transform.localScale = src.localScale;
        FinishInstance(go, f.castShadow);
    }

    /// <summary>
    /// 장식 인스턴스 공통 처리: 팩 프리팹의 Collider 제거, 그림자 설정, Static 해제.
    /// </summary>
    private static void FinishInstance(GameObject go, bool castShadow, bool receiveShadow = true)
    {
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(c);
        }
        if (!receiveShadow)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.receiveShadows = false;
            }
        }
        if (!castShadow)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        GameObjectUtility.SetStaticEditorFlags(go, 0);
    }

    private static float NormalizedSize(Family f, Transform src)
    {
        GameObject srcPrefab = PrefabUtility.GetCorrespondingObjectFromSource(src.gameObject);
        if (srcPrefab == null || f.max <= f.min)
        {
            return 0.5f;
        }
        float size = MeshSize(srcPrefab, f.useWidth) * src.localScale.y / Mathf.Max(srcPrefab.transform.localScale.y, 0.0001f);
        return Mathf.Clamp01((size - f.min) / (f.max - f.min));
    }

    private static Bounds MeshBounds(GameObject prefab)
    {
        var mf = prefab.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }
        Bounds b = mf.sharedMesh.bounds;
        Vector3 s = mf.transform.lossyScale;
        return new Bounds(Vector3.Scale(b.center, s), Vector3.Scale(b.size, s));
    }

    private static float MeshSize(GameObject prefab, bool useWidth)
    {
        Bounds b = MeshBounds(prefab);
        return useWidth ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
    }

    private static List<Vector2> FlowerPatchCenters(Transform spring)
    {
        var centers = new List<Vector2>();
        Transform field = spring.Find("Field");
        if (field == null)
        {
            return centers;
        }
        foreach (Transform roleGroup in field)
        {
            if (roleGroup.name == "Grass" || roleGroup.name == "BushBerries")
            {
                continue;
            }
            foreach (Transform t in roleGroup)
            {
                var p = new Vector2(t.position.x, t.position.z);
                if (!TooClose(centers, p, 6f))
                {
                    centers.Add(p);
                }
            }
        }
        return centers;
    }

    private static List<Vector2> PondCenters(Workspace ws)
    {
        var list = new List<Vector2>();
        foreach (Bounds b in ws.ponds)
        {
            list.Add(new Vector2(b.center.x, b.center.z));
        }
        return list;
    }

    private static List<Vector2> Take(List<Vector2> src, System.Random rnd, int count)
    {
        var copy = new List<Vector2>(src);
        for (int i = copy.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.GetRange(0, Mathf.Min(count, copy.Count));
    }

    private static (string, string, float) Pick(System.Random rnd, (string, string, float)[] items)
    {
        float total = 0f;
        foreach ((string, string, float) it in items)
        {
            total += it.Item3;
        }
        float v = (float)rnd.NextDouble() * total;
        foreach ((string, string, float) it in items)
        {
            v -= it.Item3;
            if (v <= 0f)
            {
                return it;
            }
        }
        return items[0];
    }

    private static bool TooClose(List<Vector2> pts, Vector2 p, float dist)
    {
        return MinDistance(pts, p) < dist;
    }

    private static float MinDistance(List<Vector2> pts, Vector2 p)
    {
        float best = float.MaxValue;
        foreach (Vector2 q in pts)
        {
            float d = Vector2.Distance(p, q);
            if (d < best)
            {
                best = d;
            }
        }
        return best;
    }

    private static Transform Group(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null)
        {
            return t;
        }
        var go = new GameObject(name);
        // new GameObject는 활성(열린) 씬에 먼저 생기므로, 부모를 붙이기 전에 부모가 있는 씬(미리보기 씬)으로 옮긴다
        SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static string Save(GameObject root, string season)
    {
        if (!AssetDatabase.IsValidFolder(c_outputFolder))
        {
            AssetDatabase.CreateFolder("Assets/SJT/03_Prefabs/Map(New)", "Seasonal");
        }
        string path = c_outputFolder + "/Forest_" + season + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        int count = root.GetComponentsInChildren<Transform>(true).Length;
        return c_logPrefix + $"{path} 저장 {(success ? "성공" : "실패")} (Transform {count})";
    }
}
