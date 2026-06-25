// ============================================================
// User - 서버 측 접속 유저(IPeer 구현)
// 한 세션에 대응하는 게임 로직. 수신 패킷은 IOCP 워커 스레드에서
// 즉시 타입별로 디스패치해 처리한다. 방 매칭은 GameRoomManager에 위임.
// ============================================================
public class User : IPeer
{
    Session _session;
    public int ID { get; }

    public string Username { get; private set; } = string.Empty;

    // 현재 속한 방. GameRoom.TryAdd / GameRoomManager가 설정·해제한다.
    public GameRoom? CurrentRoom { get; set; }

    public User(Session session, int id)
    {
        _session = session;
        _session.Peer = this;       // 세션에 자신을 바인딩
        ID = id;
    }

    // 수신 패킷을 서버 처리 로직으로 라우팅 (IOCP 워커 스레드)
    public void OnMessage(Packet packet)
    {
        Console.WriteLine($"[수신] {_session.Socket?.RemoteEndPoint} → {packet.GetType().Name}");

        switch (packet)
        {
            case LoginReqPacket login:
                OnLogin(login);
                break;
            case RoomEnterReqPacket:
                OnRoomEnter();
                break;
            case LoadingCompleteReqPacket:
                Program.RoomManager.OnLoadingComplete(this);
                break;
        }
    }

    // 로그인 처리 — username 저장 후 성공 응답
    void OnLogin(LoginReqPacket login)
    {
        Username = login.Username;
        Console.WriteLine($"[로그인] {Username}");

        var ack = Packet.Create<LoginAckPacket>(this);
        ack.Success = true;
        Send(ack);
    }

    // 대기방 입장 요청 — 매칭 매니저에 위임
    void OnRoomEnter() => Program.RoomManager.Enqueue(this);

    // 패킷 직렬화 후 세션으로 송신
    public void Send(Packet packet) => _session.Send(packet.Pack());

    // 이미 직렬화된 바이트 송신 (broadcast 시 한 번 Pack해 재사용)
    public void Send(byte[] data) => _session.Send(data);

    // 연결 해제 시 정리 — 속한 방에서 빼고 인원 갱신
    public void OnRemoved()
    {
        Console.WriteLine($"User 연결 해제: {Username}");
        Program.RoomManager.OnUserLeft(this);
    }

    // 능동 연결 종료
    public void Disconnected() => _session.Close();
}
