using System.Net;
using System.Net.Sockets;

// ============================================================
// Listener - 클라이언트 접속(Accept) 전담
// 전용 스레드에서 한 번에 하나씩 Accept를 처리하고(AutoResetEvent 흐름 제어),
// 접속이 들어오면 ClientEnterCallback으로 NetworkService에 넘긴다.
// ============================================================
public class Listener
{
    SocketAsyncEventArgs _acceptArgs;   // Accept 전용 SAEA
    Socket _listenSocket;               // 리슨 소켓

    AutoResetEvent _flowControlEvent;   // Accept 1건 처리 후 다음 Accept를 풀어주는 신호

    // 접속 수락 시 호출되는 콜백 (소켓을 상위로 전달)
    public delegate void OnClientEnterHandler(Socket clientSocket, object token);
    public OnClientEnterHandler ClientEnterCallback;

    // 리슨 소켓을 바인드·리슨하고 Accept 루프 스레드를 시작
    public void Start(string host, int port, int backlog)
    {
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPAddress address = host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(host);
        IPEndPoint endPoint = new IPEndPoint(address, port);

        _listenSocket.Bind(endPoint);
        _listenSocket.Listen(backlog);

        _acceptArgs = new SocketAsyncEventArgs();
        _acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

        Thread listenThread = new Thread(DoListen);
        listenThread.Start();
    }

    // Accept 루프 — 한 건 처리할 때까지 대기 후 다음 Accept를 건다
    private void DoListen()
    {
        _flowControlEvent = new AutoResetEvent(false);

        while (true)
        {
            _acceptArgs.AcceptSocket = null;

            // 비동기 Accept. 동기 완료(pending==false) 시 콜백 직접 호출
            bool pending = _listenSocket.AcceptAsync(_acceptArgs);
            if (pending == false)
                OnAcceptCompleted(null, _acceptArgs);

            _flowControlEvent.WaitOne();   // 이번 Accept 처리가 끝날 때까지 대기
        }
    }

    // Accept 완료 — 클라 소켓을 상위로 전달하고 다음 Accept를 풀어줌
    private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            Socket clientSocket = args.AcceptSocket;
            clientSocket.NoDelay = true;   // Nagle 비활성화 (지연 최소화)

            ClientEnterCallback?.Invoke(clientSocket, args);
        }
        else
        {
            Console.WriteLine("Fail to Accept Client. " + args.SocketError);
        }

        _flowControlEvent.Set();   // 다음 Accept 진행 허용
    }
}