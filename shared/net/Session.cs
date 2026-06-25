using System.Net.Sockets;

// ============================================================
// Session - 연결 1개의 송수신 상태를 담는 단위
// 송신은 락 큐로 직렬화(한 번에 하나씩 전송), 수신은 MessageResolver로
// 프레이밍해 완성된 패킷을 Peer에 전달한다. Close는 1회만 실행된다.
// ============================================================
public class Session
{
    int _isClosed = 0;                          // 중복 종료 방지 플래그 (0=열림, 1=닫힘)
    bool _isSending = false;                     // 현재 송신 진행 중인지
    readonly Queue<byte[]> _sendQueue = new();   // 대기 중인 송신 데이터 큐

    public Socket Socket;

    // 이 세션이 사용하는 수신/송신 SAEA (NetworkService가 풀에서 할당)
    public SocketAsyncEventArgs? ReceiveEventArgs;
    public SocketAsyncEventArgs? SendEventArgs;

    // 세션이 닫힐 때 발생 — NetworkService가 SAEA를 회수
    public event Action<Session> OnSessionClosed;

    MessageResolver _messageResolver;            // TCP 스트림 → 메시지 단위 복원

    public IPeer? Peer;                          // 이 세션에 바인딩된 게임 로직

    public Session(int bufferSize)
    {
        _messageResolver = new MessageResolver(bufferSize);
    }

    // 연결 종료 — Interlocked로 단 한 번만 실제 정리 수행
    public void Close()
    {
        if (Interlocked.CompareExchange(ref _isClosed, 1, 0) == 1)
            return;

        Socket.Close();
        Socket = null;

        Peer?.OnRemoved();              // 게임 로직에 제거 통보
        OnSessionClosed?.Invoke(this);  // SAEA 반납 트리거
    }

    // 송신 요청 — 큐에 넣고, 진행 중이 아니면 송신 시작 (스레드 안전)
    public void Send(byte[] data)
    {
        lock (_sendQueue)
        {
            _sendQueue.Enqueue(data);
            if (_isSending) return;     // 이미 송신 중이면 큐에만 쌓고 반환
            _isSending = true;
        }
        BeginSend();
    }

    // 큐에서 하나 꺼내 실제 비동기 송신 (없으면 송신 종료)
    private void BeginSend()
    {
        byte[] data;
        lock (_sendQueue)
        {
            if (_sendQueue.Count == 0)
            {
                _isSending = false;
                return;
            }
            data = _sendQueue.Dequeue();
        }

        var socket = Socket;
        if (socket == null) return;

        SendEventArgs.SetBuffer(data, 0, data.Length);
        socket.SendAsyncEx(SendEventArgs, ProcessSend);
    }

    // 송신 완료 — 성공이면 다음 큐 항목을 보내고, 실패면 종료
    public void ProcessSend(SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
            BeginSend();
        else
            Close();
    }

    // 수신 바이트를 MessageResolver에 넘겨 메시지 단위로 복원
    public void OnReceive(byte[] buffer, int offset, int transferred)
    {
        _messageResolver.ReceiveMessage(buffer, offset, transferred, OnMessageCompleted);
    }

    // 완성된 메시지 1개 → 패킷 파싱 후 Peer에 전달
    private void OnMessageCompleted(ArraySegment<byte> buffer)
    {
        if (Peer == null) return;
        Packet? packet = Packet.Parse(Peer, buffer);
        if (packet != null)
            Peer.OnMessage(packet);
    }
}
