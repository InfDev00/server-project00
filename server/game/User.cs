// ============================================================
// User - 서버 측 접속 유저(IPeer 구현)
// 한 세션에 대응하는 게임 로직. 서버는 수신 패킷을 IOCP 워커
// 스레드에서 바로 처리(packet.Handle())한다.
// ============================================================
public class User : IPeer
{
    Session _session;

    public User(Session session)
    {
        _session = session;
        _session.Peer = this;       // 세션에 자신을 바인딩
    }

    // 서버는 IOCP 워커 스레드에서 즉시 처리해도 무방
    public void OnMessage(Packet packet)
    {
        Console.WriteLine($"[수신] {_session.Socket?.RemoteEndPoint} → {packet.GetType().Name}");
        packet.Handle();
    }

    // 패킷 직렬화 후 세션으로 송신
    public void Send(Packet packet) => _session.Send(packet.Pack());

    // 연결 해제 시 정리
    public void OnRemoved() => Console.WriteLine("User 연결 해제");

    // 능동 연결 종료
    public void Disconnected() => _session.Close();
}