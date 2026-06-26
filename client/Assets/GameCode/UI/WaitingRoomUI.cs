using TMPro;
using UnityEngine;

// ============================================================
// WaitingRoomUI - 대기방 화면(IUI)
// 로그인 성공 후 UISystem.Next()로 전환되면 대기방 입장을 요청하고,
// 인원 변동(RoomState)을 표시, 정원이 차면(GameStart) 다음 화면으로 넘어간다.
// 수신 핸들러는 메인 스레드(Update 디스패치)에서 호출되어 UI 조작 안전.
// ============================================================
public class WaitingRoomUI : IUI
{
    public TMP_Text StatusText;   // "Room 1 - 1/2" 형태로 대기 현황 표시

    int _roomId;

    public override void OnStart()
    {
        // 대기방 관련 패킷 구독
        NetworkSystem.Instance.Register<RoomEnterAckPacket>(OnEnterAck);
        NetworkSystem.Instance.Register<RoomStateNotifyPacket>(OnRoomState);
        NetworkSystem.Instance.Register<LoadingNotifyPacket>(OnLoading);

        gameObject.SetActive(true);
        if (StatusText) StatusText.text = "Entering waiting room...";

        // 입장 요청 송신 (페이로드 없음)
        NetworkSystem.Instance.Send(NetworkSystem.Instance.CreatePacket<RoomEnterReqPacket>());
    }

    public override void OnEnd()
    {
        NetworkSystem.Instance.Unregister<RoomEnterAckPacket>(OnEnterAck);
        NetworkSystem.Instance.Unregister<RoomStateNotifyPacket>(OnRoomState);
        NetworkSystem.Instance.Unregister<LoadingNotifyPacket>(OnLoading);

        gameObject.SetActive(false);
    }

    // 입장 결과 — 배정된 방 번호 기억 + 현황 표시
    private void OnEnterAck(RoomEnterAckPacket p)
    {
        _roomId = p.RoomId;
        SystemManager.Instance.MyID = p.UID;
        UpdateStatus(p.Current, p.Needed);
    }

    // 같은 방 인원 변동 통지
    private void OnRoomState(RoomStateNotifyPacket p)
    {
        UpdateStatus(p.Current, p.Needed);
    }

    // 정원 충족 → 로딩 화면으로 전환
    private void OnLoading(LoadingNotifyPacket p)
    {
        Debug.Log($"[Loading] room={p.RoomId} → 로딩 화면 전환");
        UISystem.Instance.Next();
    }

    private void UpdateStatus(int current, int needed)
    {
        if (StatusText)
            StatusText.text = $"Room {_roomId} - waiting {current}/{needed}";
    }
}
