public class User : IPeer
{
    Session _session;

    public User(Session session)
    {
        _session = session;
        _session.Peer = this;
    }

    // 서버는 IOCP 워커 스레드에서 즉시 처리해도 무방
    public void OnMessage(Packet packet) => packet.Handle();

    public void Send(Packet packet) => _session.Send(packet.Pack());

    public void OnRemoved() => Console.WriteLine("User 연결 해제");

    public void Disconnected() => _session.Close();
}