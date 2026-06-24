using System.Net;
using UnityEngine;

public class NetworkSystem : MonoBehaviour
{
    NetworkService _service;
    ServerSession _gameServer;

    void Start()
    {
        _service = new NetworkService();
        _service.OnSessionCreated += OnConnectedGameServer;
        _service.Initialize(1, 1024);   // 클라는 연결 1개면 충분

        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7979);
        _service.Connect(endPoint);
    }

    // IOCP 워커 스레드 — 연결 완료 콜백
    private void OnConnectedGameServer(Session session)
    {
        _gameServer = new ServerSession(session);
        Debug.Log("서버 연결 성공 → 로그인 요청 전송");

        var req = Packet.Create<LoginReqPacket>(_gameServer);
        req.Username = "tester";
        _gameServer.Send(req);          // Session.Send는 락 큐라 스레드 안전
    }

    // 메인 스레드 — 수신 패킷 처리
    void Update()
    {
        if (_gameServer == null) return;

        while (_gameServer.RecvQueue.TryDequeue(out var packet))
        {
            switch (packet)
            {
                case LoginAckPacket ack:
                    Debug.Log($"[LoginAck] success={ack.Success}");
                    break;
            }
        }
    }

    void OnApplicationQuit() => _gameServer?.Disconnected();
}
