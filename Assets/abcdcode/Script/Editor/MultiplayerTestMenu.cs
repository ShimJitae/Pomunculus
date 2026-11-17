using System.IO;
using UnityEditor;
using UnityEngine;

// 멀티 테스트 프로필 저장 폴더 관리 메뉴
// 메뉴는 각 에디터(메인/가상 플레이어)에서 자기 프로필 기준으로 동작함
public static class MultiplayerTestMenu
{
    private const string Root = "Tools/Multiplayer Test/";

    [MenuItem(Root + "현재 프로필 저장 폴더 열기")]
    private static void OpenCurrentProfileFolder()
    {
        EditorUtility.RevealInFinder(MultiplayerTestProfile.SaveDirectory + "/");
    }

    [MenuItem(Root + "현재 프로필 저장 데이터 삭제")]
    private static void DeleteCurrentProfile()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[MultiplayerTest] 플레이 중에는 삭제할 수 없음");
            return;
        }
        string dir = MultiplayerTestProfile.SaveDirectory;
        if (!EditorUtility.DisplayDialog("저장 데이터 삭제",
                $"프로필 '{MultiplayerTestProfile.Name}'의 저장 파일을 모두 삭제합니다.\n{dir}", "삭제", "취소")) return;

        // 기본 프로필 폴더(persistentDataPath)에는 다른 데이터가 있을 수 있으므로 파일만 지우고 Profiles 폴더는 남김
        foreach (string file in Directory.GetFiles(dir)) File.Delete(file);
        Debug.Log($"[MultiplayerTest] 프로필 '{MultiplayerTestProfile.Name}' 저장 데이터 삭제");
    }

    [MenuItem(Root + "가상 플레이어 프로필 전체 삭제")]
    private static void DeleteAllVirtualProfiles()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[MultiplayerTest] 플레이 중에는 삭제할 수 없음");
            return;
        }
        string dir = Path.Combine(Application.persistentDataPath, MultiplayerTestProfile.ProfileRootDir);
        if (!Directory.Exists(dir))
        {
            Debug.Log("[MultiplayerTest] 삭제할 프로필이 없음");
            return;
        }
        if (!EditorUtility.DisplayDialog("프로필 전체 삭제",
                $"메인 프로필을 제외한 모든 테스트 프로필을 삭제합니다.\n{dir}", "삭제", "취소")) return;
        Directory.Delete(dir, true);
        Debug.Log("[MultiplayerTest] 테스트 프로필 전체 삭제");
    }

    [MenuItem(Root + "현재 프로필 정보 출력")]
    private static void LogProfile()
    {
        Debug.Log($"[MultiplayerTest] 프로필 : {MultiplayerTestProfile.Name}, 가상 플레이어 : {MultiplayerTestProfile.IsVirtualPlayer}, 저장 폴더 : {MultiplayerTestProfile.SaveDirectory}");
    }
}
