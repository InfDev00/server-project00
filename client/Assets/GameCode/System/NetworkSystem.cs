using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

// ============================================================
// NetworkSystem - 클라이언트 네트워크 진입점(MonoBehaviour)
// 시작 시 서버에 연결해 로그인 요청을 보내고, 수신 패킷은
// 워커 스레드 큐에 쌓인 것을 Update(메인 스레드)에서 처리한다.
// ============================================================
public class NetworkSystem : MonoBehaviour
{
    static NetworkSystem _instance;
    public static NetworkSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindFirstObjectByType<NetworkSystem>();
            }
            return _instance;
        }
    }

    NetworkService _service;        // 공용 네트워크 계층
    ServerSession _gameServer;      // 게임 서버와의 세션(IPeer)

    // 패킷 타입 → 디스패치용 핸들러(래퍼). 일반 호출이라 리플렉션 없음.
    readonly Dictionary<Type, Action<Packet>> _handlers = new();

    // 원본 핸들러 → 래퍼. 구독 해제 시 어느 래퍼를 뺄지 찾기 위함.
    readonly Dictionary<Delegate, Action<Packet>> _wrappers = new();

    void Start()
    {
        Application.runInBackground = true;

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
    }

    public T CreatePacket<T>() where T : Packet, new() => Packet.Create<T>(_gameServer);

    public void Send(Packet packet)
    {
        if (_gameServer == null)
            return;

        _gameServer.Send(packet);
    }

    // 특정 패킷 타입에 핸들러 등록 (UI 등이 OnEnable에서 호출)
    public void Register<T>(Action<T> handler) where T : Packet
    {
        if (_wrappers.ContainsKey(handler)) return;        // 중복 등록 방지

        // T를 아는 지금 캐스팅을 래퍼에 가둔다 → 디스패치 때 일반 호출 가능
        Action<Packet> wrapper = p => handler((T)p);
        _wrappers[handler] = wrapper;

        _handlers.TryGetValue(typeof(T), out var cur);
        _handlers[typeof(T)] = cur + wrapper;              // cur가 null이어도 OK
    }

    // 핸들러 해제 (OnDisable에서 호출 — 파괴된 객체 호출 방지)
    public void Unregister<T>(Action<T> handler) where T : Packet
    {
        if (!_wrappers.TryGetValue(handler, out var wrapper)) return;
        _wrappers.Remove(handler);

        if (!_handlers.TryGetValue(typeof(T), out var cur)) return;

        var next = cur - wrapper;
        if (next == null) _handlers.Remove(typeof(T));
        else _handlers[typeof(T)] = next;
    }

    // 메인 스레드 — 수신 패킷을 등록된 핸들러로 일반 호출 디스패치(리플렉션 없음)
    void Update()
    {
        if (_gameServer == null) return;

        while (_gameServer.RecvQueue.TryDequeue(out var packet))
        {
            if (_handlers.TryGetValue(packet.GetType(), out var handler))
            {
                handler(packet);
            }
        }
    }

    // 앱 종료 시 연결 정리
    void OnApplicationQuit() => _gameServer?.Disconnected();
}
