using System.Collections.Concurrent;

public class ServerSession : IPeer
{
    Session _session;

    // IOCP 워커 스레드에서 받은 패킷을 메인 스레드(Update)로 넘기기 위한 큐
    public ConcurrentQueue<Packet> RecvQueue = new ConcurrentQueue<Packet>();

    public ServerSession(Session session)
    {
        _session = session;
        _session.Peer = this;
    }

    public void OnMessage(Packet packet) => RecvQueue.Enqueue(packet);  // 즉시 처리 X, 큐로만
    public void Send(Packet packet) => _session.Send(packet.Pack());
    public void OnRemoved() => UnityEngine.Debug.Log("서버 연결 종료");
    public void Disconnected() => _session.Close();
}