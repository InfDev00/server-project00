using System.Net;
using UnityEngine;

// ============================================================
// NetworkSystem - 클라이언트 네트워크 진입점(MonoBehaviour)
// 시작 시 서버에 연결해 로그인 요청을 보내고, 수신 패킷은
// 워커 스레드 큐에 쌓인 것을 Update(메인 스레드)에서 처리한다.
// ============================================================
public class NetworkSystem : MonoBehaviour
{
    NetworkService _service;        // 공용 네트워크 계층
    ServerSession _gameServer;      // 게임 서버와의 세션(IPeer)

    void Start()
    {
        _service = new NetworkService();
        _service.OnSessionCreated += OnConnectedGameServer;
        _service.Initialize(1, 1024);   // 클라는 연결 1개면 충분

        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7979);
        _service.Connect(endPoint);     // 로컬 서버로 연결 시도
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

    // 앱 종료 시 연결 정리
    void OnApplicationQuit() => _gameServer?.Disconnected();
}
