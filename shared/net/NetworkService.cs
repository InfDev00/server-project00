using System.Net;
using System.Net.Sockets;

// ============================================================
// NetworkService - 네트워크 계층 진입점(서버/클라 공용)
// SAEA 풀과 버퍼를 미리 할당하고, 접속(Listen)·연결(Connect)부터
// 수신/송신 비동기 루프까지 세션 수명주기 전반을 관리한다.
// ============================================================
public class NetworkService
{
    // 미리 할당해 재사용하는 수신/송신용 SAEA 풀 (GC 최소화)
    private SocketAsyncEventArgsPool _receiveEventArgsPool;
    private SocketAsyncEventArgsPool _sendEventArgsPool;

    // 모든 수신 SAEA가 나눠 쓰는 단일 연속 버퍼
    private BufferManager _bufferManager;
    int _bufferSize;

    // 세션 생성 시(접속/연결 완료) 호출되는 콜백 — 상위 계층이 IPeer를 바인딩
    public Action<Session> OnSessionCreated;

    public NetworkService()
    {
        Packet.Register();          // 패킷 파서 등록 (수신 시 ID→타입 매핑)
        OnSessionCreated = null;
    }

    // 기본값으로 초기화 (최대 1만 연결, 버퍼 1KB)
    public void Initialize() => Initialize(10000, 1024);

    // SAEA 풀과 버퍼를 maxConnection 개수만큼 미리 할당
    public void Initialize(int maxConnection, int bufferSize)
    {
        _bufferSize = bufferSize;

        int preAllocCount = 1;

        _bufferManager = new BufferManager(maxConnection * bufferSize * preAllocCount, bufferSize);
        _receiveEventArgsPool = new SocketAsyncEventArgsPool(maxConnection);
        _sendEventArgsPool = new SocketAsyncEventArgsPool(maxConnection);

        // 연결 1개당 수신 SAEA 1개 + 송신 SAEA 1개를 풀에 미리 채워둔다
        for (int i = 0; i < maxConnection; ++i)
        {
            // 수신 SAEA: BufferManager에서 버퍼 구간을 할당받음
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
                args.UserToken = null;

                _bufferManager.SetBuffer(args);
                _receiveEventArgsPool.Push(args);
            }

            // 송신 SAEA: 보낼 때마다 버퍼를 직접 SetBuffer 하므로 빈 채로 풀에 넣음
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
                args.UserToken = null;

                args.SetBuffer(null, 0, 0);
                _sendEventArgsPool.Push(args);
            }
        }
    }

    // 서버: 리스너를 띄워 클라이언트 접속을 받는다
    public void Listen(string host, int port, int backlog)
    {
        Listener clientListener = new Listener();
        clientListener.ClientEnterCallback += OnNewClientEnter;
        clientListener.Start(host, port, backlog);
    }

    // 클라: 원격 서버로 비동기 연결을 시도한다
    public void Connect(IPEndPoint remoteEndpoint)
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket.NoDelay = true;

        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
        args.Completed += OnConnectCompleted;
        args.RemoteEndPoint = remoteEndpoint;

        clientSocket.ConnectAsyncEx(args, OnConnectCompleted);
    }

    // 클라: 연결 완료 콜백 — 세션을 만들고 수신 루프를 시작
    private void OnConnectCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            Socket socket = args.ConnectSocket;

            Session session = new Session(1024);
            session.OnSessionClosed += OnSessionClosed;

            // 클라는 풀을 쓰지 않고 SAEA를 직접 생성 (연결 1개뿐)
            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            receiveArgs.UserToken = session;
            receiveArgs.SetBuffer(new byte[1024], 0, 1024);

            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            sendArgs.UserToken = session;
            sendArgs.SetBuffer(null, 0, 0);

            // OnSessionCreated 전에 세션 세팅 완료 — 콜백 안에서 Send() 즉시 사용 가능
            session.ReceiveEventArgs = receiveArgs;
            session.SendEventArgs = sendArgs;
            session.Socket = socket;

            OnSessionCreated?.Invoke(session);

            socket.ReceiveAsyncEx(receiveArgs, ProcessReceive);   // 수신 대기 시작
        }
    }

    // 서버: 새 클라이언트 접속 콜백 — 풀에서 SAEA를 꺼내 세션 구성
    private void OnNewClientEnter(Socket clientSocket, object token)
    {
        SocketAsyncEventArgs receiveArgs = _receiveEventArgsPool.Pop();
        SocketAsyncEventArgs sendArgs = _sendEventArgsPool.Pop();

        Session session = new(_bufferSize);
        session.OnSessionClosed += OnSessionClosed;

        receiveArgs.UserToken = session;
        sendArgs.UserToken = session;

        // OnSessionCreated 전에 세션 세팅 완료
        session.ReceiveEventArgs = receiveArgs;
        session.SendEventArgs = sendArgs;
        session.Socket = clientSocket;

        OnSessionCreated?.Invoke(session);

        clientSocket.ReceiveAsyncEx(receiveArgs, ProcessReceive);   // 수신 대기 시작
    }

    // 송신 완료 콜백 — 세션의 후속 송신 처리로 위임
    private void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.TryGetSession(out Session? session))
        {
            session!.ProcessSend(args);
        }
    }

    // 수신 완료 콜백 (SAEA.Completed 이벤트 경유)
    private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.LastOperation == SocketAsyncOperation.Receive)
        {
            ProcessReceive(args);
        }
    }

    // 수신 데이터 처리 후 다음 수신을 다시 건다(수신 루프)
    private void ProcessReceive(SocketAsyncEventArgs args)
    {
        if (args.TryGetSession(out Session? session))
        {
            // 정상 수신: 세션에 넘기고 다음 수신 예약
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                session!.OnReceive(args.Buffer, args.Offset, args.BytesTransferred);

                session.Socket.ReceiveAsyncEx(args, ProcessReceive);
            }
            // 0바이트 또는 에러: 상대가 끊은 것이므로 세션 종료
            else
            {
                session!.Close();
            }
        }
    }

    // 세션 종료 콜백 — 사용하던 SAEA를 풀에 반납
    private void OnSessionClosed(Session session)
    {
        _receiveEventArgsPool?.Push(session.ReceiveEventArgs);
        _sendEventArgsPool?.Push(session.SendEventArgs);

        session.ReceiveEventArgs = null;
        session.SendEventArgs = null;
    }
}
