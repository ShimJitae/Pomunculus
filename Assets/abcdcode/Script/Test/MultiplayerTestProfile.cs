using System;
using System.IO;
using UnityEngine;

// 한 PC에서 여러 인스턴스로 멀티 테스트할 때 인스턴스별 프로필(저장 폴더)을 구분
// 모든 인스턴스가 같은 persistentDataPath를 쓰기 때문에, 구분하지 않으면 UID/월드 슬롯이 전부 같아져 같은 플레이어로 취급됨
//
// 판별 순서
// 1. 실행 인자 -profile <이름>  (빌드를 여러 개 띄워 테스트할 때)
// 2. Multiplayer Play Mode 가상 플레이어 : 클론 에디터는 <프로젝트>/Library/VP/<폴더>/ 에서 실행되므로 그 폴더 이름 사용
// 3. 그 외(메인 에디터, 일반 빌드)는 기본 프로필 -> 기존 저장 경로 그대로
public static class MultiplayerTestProfile
{
    public const string DefaultName = "Main";
    private const string ProfileArg = "-profile";
    private const string VirtualPlayerDir = "/Library/VP/";
    public const string ProfileRootDir = "Profiles";

    private static string s_name;
    public static string Name => s_name ??= Detect();
    public static bool IsDefault => Name == DefaultName;
    public static bool IsVirtualPlayer => Application.isEditor && Application.dataPath.Replace('\\', '/').Contains(VirtualPlayerDir);

    // 기본 프로필은 기존 경로, 그 외는 persistentDataPath/Profiles/<이름>
    public static string SaveDirectory
    {
        get
        {
            if (IsDefault) return Application.persistentDataPath;
            string dir = Path.Combine(Application.persistentDataPath, ProfileRootDir, Name);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string Detect()
    {
        string fromArg = GetArg(ProfileArg);
        if (!string.IsNullOrWhiteSpace(fromArg)) return Sanitize(fromArg);

        string dataPath = Application.dataPath.Replace('\\', '/');
        int idx = dataPath.IndexOf(VirtualPlayerDir, StringComparison.Ordinal);
        if (idx >= 0)
        {
            string rest = dataPath.Substring(idx + VirtualPlayerDir.Length);
            int slash = rest.IndexOf('/');
            string folder = slash >= 0 ? rest.Substring(0, slash) : rest;
            if (!string.IsNullOrEmpty(folder)) return Sanitize(folder);
        }
        return DefaultName;
    }
    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        }
        return null;
    }
    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}
