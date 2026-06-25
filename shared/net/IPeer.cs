// ============================================================
// IPeer - 세션과 게임 로직을 연결하는 콜백 인터페이스
// 한 Session에 구현체 하나를 바인딩(session.Peer)해 수신/해제 이벤트를 처리한다.
// 서버는 User, 클라는 ServerSession이 구현한다.
// ============================================================
public interface IPeer
{
    // 완성된 패킷 1개 수신 시 호출 (IOCP 워커 스레드)
    void OnMessage(Packet packet);

    // 세션이 닫혀 연결이 제거될 때 호출
    void OnRemoved();

    // 패킷을 직렬화해 상대에게 전송
    void Send(Packet packet);

    // 능동적으로 연결을 끊을 때 호출
    void Disconnected();
}