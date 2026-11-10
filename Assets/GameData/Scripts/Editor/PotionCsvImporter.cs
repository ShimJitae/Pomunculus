using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Pomunculus.Items.Editor
{
    public static class PotionCsvImporter
    {
        private const string CsvPath = "Assets/GameData/CSV/Potions.csv";
        private const string OutputFolder = "Assets/GameData/Items/Potions";
        private const string DatabasePath = "Assets/Resources/PotionDatabase.asset";

        private class PotionRow
        {
            public int Id;
            public string Name;
            public string Description;
            public Sprite Icon;
            public float PhysicsElement;
            public float FireElement;
            public float ColdElement;
            public float WindElement;
        }

        [MenuItem("Tools/GameData/포션 CSV 가져오기")]
        public static void Import()
        {
            try
            {
                if (!File.Exists(CsvPath))
                    throw new Exception($"CSV 파일을 찾을 수 없습니다.\n{CsvPath}");

                string csv = File.ReadAllText(CsvPath, Encoding.UTF8);
                List<PotionRow> rows = ReadPotions(ParseCsv(csv));

                EnsureFolder(OutputFolder);

                Dictionary<int, PotionData> existing = FindExistingPotions();
                ValidateOutputPaths(rows, existing);

                int created = 0;
                int updated = 0;

                foreach (PotionRow row in rows)
                {
                    string targetPath = $"{OutputFolder}/{row.Id}_{row.Name}.asset";
                    bool isNew = !existing.TryGetValue(row.Id, out PotionData potion);

                    if (isNew)
                    {
                        potion = ScriptableObject.CreateInstance<PotionData>();
                    }
                    else
                    {
                        string currentPath = AssetDatabase.GetAssetPath(potion);

                        if (currentPath != targetPath)
                        {
                            string error = AssetDatabase.MoveAsset(currentPath, targetPath);

                            if (!string.IsNullOrEmpty(error))
                                throw new Exception($"파일 이름을 변경하지 못했습니다.\n{error}");
                        }

                        Undo.RecordObject(potion, "포션 CSV 가져오기");
                    }

                    potion.Id = row.Id;
                    potion.ItemName = row.Name;
                    potion.Description = row.Description;
                    potion.Icon = row.Icon;
                    potion.PhysicsElement = row.PhysicsElement;
                    potion.FireElement = row.FireElement;
                    potion.ColdElement = row.ColdElement;
                    potion.WindElement = row.WindElement;
                    potion.name = $"{row.Id}_{row.Name}";

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(potion, targetPath);
                        created++;
                    }
                    else
                    {
                        EditorUtility.SetDirty(potion);
                        updated++;
                    }
                }

                AssetDatabase.SaveAssets();
                UpdatePotionDatabase();
                AssetDatabase.Refresh();

                string message = $"새로 생성: {created}개\n기존 수정: {updated}개\n\n저장 위치: {OutputFolder}";
                EditorUtility.DisplayDialog("포션 CSV 가져오기 완료", message, "확인");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("포션 CSV 가져오기 실패", exception.Message, "확인");
            }
        }

        // 프로젝트의 모든 PotionData를 찾아 PotionDatabase 목록을 다시 채운다
        [MenuItem("Tools/GameData/포션 DB 갱신")]
        public static void UpdatePotionDatabase()
        {
            PotionDatabase database = AssetDatabase.LoadAssetAtPath<PotionDatabase>(DatabasePath);

            if (database == null)
            {
                EnsureFolder(Path.GetDirectoryName(DatabasePath).Replace('\\', '/'));
                database = ScriptableObject.CreateInstance<PotionDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            List<PotionData> potions = new List<PotionData>();
            HashSet<int> ids = new HashSet<int>();

            foreach (string guid in AssetDatabase.FindAssets("t:PotionData"))
            {
                PotionData potion = AssetDatabase.LoadAssetAtPath<PotionData>(AssetDatabase.GUIDToAssetPath(guid));

                if (potion == null)
                    continue;

                // 중복 Id는 예외 대신 오류 로그만 남기고 DB에서 뺀다 (CSV 가져오기 전체가 실패하지 않게)
                if (!ids.Add(potion.Id))
                {
                    Debug.LogError($"같은 ID의 포션이 여러 개 있어 DB에서 제외합니다: {potion.Id} ({AssetDatabase.GetAssetPath(potion)})", potion);
                    continue;
                }

                potions.Add(potion);
            }

            potions.Sort((a, b) => a.Id.CompareTo(b.Id));

            // potions가 private 필드라 SerializedObject로 채운다
            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty list = serializedDatabase.FindProperty("potions");
            list.arraySize = potions.Count;

            for (int i = 0; i < potions.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = potions[i];

            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private static List<PotionRow> ReadPotions(List<List<string>> table)
        {
            if (table.Count == 0)
                throw new Exception("CSV 파일이 비어 있습니다.");

            Dictionary<string, int> columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < table[0].Count; i++)
            {
                string column = table[0][i].Trim().TrimStart('\uFEFF');

                if (string.IsNullOrWhiteSpace(column))
                    throw new Exception("첫 번째 행에 빈 열 이름이 있습니다. 비어 있는 열을 삭제해 주세요.");

                if (columns.ContainsKey(column))
                    throw new Exception($"중복된 열 이름입니다: {column}");

                columns.Add(column, i);
            }

            foreach (string required in new[] { "Id", "Name", "Desc", "icon_path", "PhysicsElement", "FireElement", "ColdElement", "WindElement" })
            {
                if (!columns.ContainsKey(required))
                    throw new Exception($"필수 열이 없습니다: {required}");
            }

            List<PotionRow> result = new List<PotionRow>();
            HashSet<int> ids = new HashSet<int>();

            for (int i = 1; i < table.Count; i++)
            {
                List<string> cells = table[i];

                if (cells.TrueForAll(string.IsNullOrWhiteSpace))
                    continue;

                if (cells.Count != table[0].Count)
                    throw new Exception($"{i + 1}행의 열 개수가 첫 번째 행과 다릅니다.");

                string idText = cells[columns["Id"]].Trim();

                if (!int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0)
                    throw new Exception($"{i + 1}행의 Id는 양의 정수여야 합니다: {idText}");

                if (!ids.Add(id))
                    throw new Exception($"중복된 포션 ID입니다: {id}");

                string itemName = cells[columns["Name"]].Trim();

                if (string.IsNullOrWhiteSpace(itemName))
                    throw new Exception($"ID {id}: 포션 이름이 비어 있습니다.");

                if (itemName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new Exception($"ID {id}: 이름에 파일 이름으로 사용할 수 없는 문자가 있습니다.");

                if (!columns.ContainsKey("icon_path"))
                    throw new Exception("필수 열이 없습니다: icon_path");

                string iconPath = cells[columns["icon_path"]].Trim().Replace('\\', '/');

                if (string.IsNullOrWhiteSpace(iconPath))
                    throw new Exception($"ID {id}: icon_path가 비어 있습니다.");

                Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                if (icon == null)
                    throw new Exception($"ID {id}: 아이콘을 불러올 수 없습니다.\n경로와 이미지의 Sprite 설정을 확인하세요.\n{iconPath}");

                float Number(string column)
                {
                    string value = cells[columns[column]].Trim();

                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) || float.IsNaN(number) || float.IsInfinity(number))
                        throw new Exception($"ID {id}: {column} 값이 숫자가 아닙니다: {value}");

                    return number;
                }
                result.Add(new PotionRow
                {
                    Id = id,
                    Name = itemName,
                    Description = cells[columns["Desc"]].Trim(),
                    Icon = icon,
                    PhysicsElement = Number("PhysicsElement"),
                    FireElement = Number("FireElement"),
                    ColdElement = Number("ColdElement"),
                    WindElement = Number("WindElement")
                });
            }

            if (result.Count == 0)
                throw new Exception("가져올 포션 데이터가 없습니다.");

            return result;
        }

        private static Dictionary<int, PotionData> FindExistingPotions()
        {
            Dictionary<int, PotionData> result = new Dictionary<int, PotionData>();
            string[] guids = AssetDatabase.FindAssets("t:PotionData", new[] { OutputFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PotionData potion = AssetDatabase.LoadAssetAtPath<PotionData>(path);

                if (potion == null)
                    continue;

                if (result.ContainsKey(potion.Id))
                    throw new Exception($"저장 폴더에 같은 ID의 포션이 여러 개 있습니다: {potion.Id}");

                result.Add(potion.Id, potion);
            }

            return result;
        }

        private static void ValidateOutputPaths(List<PotionRow> rows, Dictionary<int, PotionData> existing)
        {
            foreach (PotionRow row in rows)
            {
                string path = $"{OutputFolder}/{row.Id}_{row.Name}.asset";
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);

                if (asset == null)
                    continue;

                if (!existing.TryGetValue(row.Id, out PotionData potion) || asset != potion)
                    throw new Exception($"저장하려는 경로에 다른 파일이 있습니다.\n{path}");
            }
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static List<List<string>> ParseCsv(string text)
        {
            List<List<string>> table = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder cell = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];

                if (character == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (character == ',' && !inQuotes)
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else if ((character == '\r' || character == '\n') && !inQuotes)
                {
                    if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    row.Add(cell.ToString());
                    table.Add(row);
                    row = new List<string>();
                    cell.Clear();
                }
                else
                {
                    cell.Append(character);
                }
            }

            if (inQuotes)
                throw new Exception("CSV의 큰따옴표가 닫히지 않았습니다.");

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                table.Add(row);
            }

            return table;
        }
    }
}