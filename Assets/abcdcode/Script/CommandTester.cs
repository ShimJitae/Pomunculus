using UnityEngine;

// 채팅 명령어 테스트용
//  /killself : 내 플레이어를 사망시킴
public class CommandTester : MonoBehaviour
{
    public void Start()
    {
        ChatManager.Instance.OnCommandEvent += OnCommand;
    }
    // ChatManager는 씬이 바뀌어도 남아 있으므로 반드시 해제
    private void OnDestroy()
    {
        var chat = ChatManager.Instance;
        if (chat != null) chat.OnCommandEvent -= OnCommand;
    }

    // 명령어 이름은 ChatManager가 소문자로 바꿔서 전달함
    private void OnCommand(string command, string[] args)
    {
        switch (command)
        {
            case "killself":
                KillSelf();
                break;
        }
    }

    private void KillSelf()
    {
        var player = FindMyPlayer();
        if (player == null)
        {
            ChatManager.Instance.Notify("내 플레이어를 찾을 수 없습니다.");
            return;
        }
        if (player.IsDead)
        {
            ChatManager.Instance.Notify("이미 사망한 상태입니다.");
            return;
        }
        // 최대 HP만큼 피해. TakeDamage를 거치므로 HP 동기화/사망 처리가 그대로 적용됨
        player.KillEntity();
        // 무적(회피 중 등)이거나 받는 피해 감소로 살아남을 수 있음
        if (!player.IsDead) ChatManager.Instance.Notify("무적 상태 등의 이유로 사망하지 않았습니다.");
    }
    private static PlayerFSM FindMyPlayer()
    {
        foreach (var obj in WorldManager.Instance.Objects.GetList())
            if (obj is PlayerFSM player && player.IsMine) return player;
        return null;
    }
}
