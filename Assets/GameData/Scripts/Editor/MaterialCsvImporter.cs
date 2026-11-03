using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Pomunculus.Items.Editor
{
    public static class MaterialCsvImporter
    {
        private const string CsvPath = "Assets/GameData/CSV/Materials.csv";
        private const string OutputFolder = "Assets/GameData/Items/Materials";
        private const string MenuPath = "Tools/GameData/재료 CSV 가져오기";

        private class MaterialRow
        {
            public int Id;
            public string Name;
            public string Description;
            public float Physics;
            public float Fire;
            public float Cold;
            public float Wind;
            public string AssetPath;
            public Sprite Icon;
        }

        [MenuItem(MenuPath)]
        public static void Import()
        {
            try
            {
                if (!File.Exists(CsvPath))
                    throw new Exception("CSV 파일이 없습니다:\n" + CsvPath);

                string text = File.ReadAllText(CsvPath, new UTF8Encoding(false, true));

                var rows = ReadMaterials(ParseCsv(text));
                var existing = FindExistingMaterials();

                ValidatePaths(rows, existing);
                EnsureFolder(OutputFolder);

                int created = 0;
                int updated = 0;
                int renamed = 0;

                foreach (var row in rows)
                {
                    if (!existing.TryGetValue(row.Id, out var item))
                    {
                        item = ScriptableObject.CreateInstance<MaterialData>();

                        ApplyData(item, row);
                        AssetDatabase.CreateAsset(item, row.AssetPath);
                        Undo.RegisterCreatedObjectUndo(item, "재료 생성");

                        created++;
                    }
                    else
                    {
                        string currentPath = AssetDatabase.GetAssetPath(item);

                        if (currentPath != row.AssetPath)
                        {
                            string error = AssetDatabase.MoveAsset(currentPath, row.AssetPath);

                            if (!string.IsNullOrEmpty(error))
                                throw new Exception($"ID {row.Id}: 이름 변경 실패\n{error}");

                            renamed++;
                        }

                        Undo.RecordObject(item, "재료 정보 갱신");
                        ApplyData(item, row);

                        updated++;
                    }

                    EditorUtility.SetDirty(item);
                }

                AssetDatabase.SaveAssets();

                string message = $"새로 생성: {created}개\n기존 갱신: {updated}개\n파일 이름 변경: {renamed}개";

                Debug.Log("재료 CSV 가져오기 완료\n" + message);
                EditorUtility.DisplayDialog("재료 CSV 가져오기 완료", message, "확인");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("재료 CSV 가져오기 실패", exception.Message, "확인");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool CanImport()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        }

        private static void ApplyData(MaterialData item, MaterialRow row)
        {
            item.Id = row.Id;
            item.ItemName = row.Name;
            item.Description = row.Description;
            item.PhysicsElement = row.Physics;
            item.FireElement = row.Fire;
            item.ColdElement = row.Cold;
            item.WindElement = row.Wind;
            item.Icon = row.Icon;

        }

        private static string GetAssetPath(int id, string itemName)
        {
            string safeName = itemName.Trim();

            const string invalidCharacters = "<>:\"/\\|?*";

            foreach (char character in invalidCharacters)
                safeName = safeName.Replace(character, '_');

            foreach (char character in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(character, '_');

            safeName = safeName.TrimEnd(' ', '.');

            if (string.IsNullOrWhiteSpace(safeName))
                throw new Exception("파일 이름으로 사용할 수 없는 재료 이름입니다.");

            return $"{OutputFolder}/{id}_{safeName}.asset";
        }

        private static Dictionary<int, MaterialData> FindExistingMaterials()
        {
            var result = new Dictionary<int, MaterialData>();

            if (!AssetDatabase.IsValidFolder(OutputFolder))
                return result;

            string[] guids = AssetDatabase.FindAssets("t:MaterialData", new[] { OutputFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<MaterialData>(path);

                if (item == null)
                    continue;

                if (result.ContainsKey(item.Id))
                    throw new Exception($"기존 재료 에셋에 중복 ID가 있습니다: {item.Id}");

                result.Add(item.Id, item);
            }

            return result;
        }

        private static void ValidatePaths(List<MaterialRow> rows, Dictionary<int, MaterialData> existing)
        {
            var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                row.AssetPath = GetAssetPath(row.Id, row.Name);

                if (!usedPaths.Add(row.AssetPath))
                    throw new Exception($"같은 파일 이름이 되는 재료가 있습니다: {row.Name}\n재료 이름을 서로 다르게 지정해 주세요.");

                existing.TryGetValue(row.Id, out var currentItem);

                var targetAsset = AssetDatabase.LoadMainAssetAtPath(row.AssetPath);

                if (targetAsset != null && targetAsset != currentItem)
                    throw new Exception($"다른 에셋이 이미 이 이름을 사용하고 있습니다:\n{row.AssetPath}\n이름이 겹치는 에셋을 먼저 확인해 주세요.");

                if (targetAsset == null && File.Exists(row.AssetPath))
                    throw new Exception("같은 위치에 파일이 이미 있습니다:\n" + row.AssetPath);
            }
        }

        private static List<MaterialRow> ReadMaterials(List<string[]> table)
        {
            if (table.Count < 2)
                throw new Exception("CSV에 열 이름과 재료 데이터가 필요합니다.");

            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < table[0].Length; i++)
            {
                string header = table[0][i].Trim().TrimStart('\uFEFF');

                if (string.IsNullOrEmpty(header))
                    throw new Exception("첫 번째 행에 빈 열 이름이 있습니다.");

                if (columns.ContainsKey(header))
                    throw new Exception("중복된 열 이름: " + header);

                columns.Add(header, i);
            }

            string[] required = { "Id", "Name", "PhysicsElement", "FireElement", "ColdElement", "WindElement", "Desc", "icon_path" };

            foreach (string column in required)
            {
                if (!columns.ContainsKey(column))
                    throw new Exception("필수 열이 없습니다: " + column);
            }

            var result = new List<MaterialRow>();
            var ids = new HashSet<int>();

            for (int i = 1; i < table.Count; i++)
            {
                string[] cells = table[i];

                if (Array.TrueForAll(cells, string.IsNullOrWhiteSpace))
                    continue;

                int rowNumber = i + 1;

                if (cells.Length != table[0].Length)
                    throw new Exception($"{rowNumber}번째 데이터 행의 열 개수가 첫 번째 행과 다릅니다.");

                string Get(string column)
                {
                    return cells[columns[column]];
                }

                if (!int.TryParse(Get("Id").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0)
                    throw new Exception($"{rowNumber}번째 데이터 행: ID는 양의 정수여야 합니다.");

                if (!ids.Add(id))
                    throw new Exception($"CSV에 중복 ID가 있습니다: {id}");

                string itemName = Get("Name").Trim();

                if (string.IsNullOrWhiteSpace(itemName))
                    throw new Exception($"ID {id}: 이름이 비어 있습니다.");

                float Number(string column)
                {
                    string value = Get(column).Trim();

                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) || float.IsNaN(number) || float.IsInfinity(number))
                        throw new Exception($"ID {id}: {column} 값이 숫자가 아닙니다: {value}");

                    return number;
                }

                string iconPath = Get("icon_path").Trim().Replace('\\', '/');

                if (string.IsNullOrWhiteSpace(iconPath))
                    throw new Exception($"ID {id}: icon_path가 비어 있습니다.");

                Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                if (icon == null)
                    throw new Exception($"ID {id}: 아이콘을 불러올 수 없습니다.\n경로와 이미지의 Sprite 설정을 확인하세요.\n{iconPath}");

                result.Add(new MaterialRow
                {
                    Id = id,
                    Name = itemName,
                    Description = Get("Desc"),
                    Physics = Number("PhysicsElement"),
                    Fire = Number("FireElement"),
                    Cold = Number("ColdElement"),
                    Wind = Number("WindElement"),
                    Icon = icon
                });
            }

            if (result.Count == 0)
                throw new Exception("가져올 재료 데이터가 없습니다.");

            return result;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
        private static List<string[]> ParseCsv(string text)
        {
            var table = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();

            bool quoted = false;
            bool closedQuote = false;

            void FinishField()
            {
                row.Add(field.ToString());
                field.Clear();
                closedQuote = false;
            }

            void FinishRow()
            {
                FinishField();
                table.Add(row.ToArray());
                row.Clear();
            }

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];

                if (i == 0 && character == '\uFEFF')
                    continue;

                if (quoted)
                {
                    if (character == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                            closedQuote = true;
                        }
                    }
                    else
                    {
                        field.Append(character);
                    }

                    continue;
                }

                if (character == ',')
                {
                    FinishField();
                }
                else if (character == '\r' || character == '\n')
                {
                    if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    FinishRow();
                }
                else if (closedQuote)
                {
                    if (!char.IsWhiteSpace(character))
                        throw new Exception("닫는 따옴표 뒤에 잘못된 문자가 있습니다.");
                }
                else if (character == '"')
                {
                    if (field.Length != 0)
                        throw new Exception("CSV의 따옴표 위치가 올바르지 않습니다.");

                    quoted = true;
                }
                else
                {
                    field.Append(character);
                }
            }

            if (quoted)
                throw new Exception("CSV에 닫히지 않은 따옴표가 있습니다.");

            if (field.Length > 0 || row.Count > 0 || closedQuote)
                FinishRow();

            return table;
        }
    }
}