using System.Net.Sockets;

public class Session
{
    int _isClosed = 0;
    bool _isSending = false;
    readonly Queue<byte[]> _sendQueue = new();

    public Socket Socket;

    public SocketAsyncEventArgs? ReceiveEventArgs;
    public SocketAsyncEventArgs? SendEventArgs;

    public event Action<Session> OnSessionClosed;

    MessageResolver _messageResolver;

    public IPeer? Peer;

    public Session(int bufferSize)
    {
        _messageResolver = new MessageResolver(bufferSize);
    }

    public void Close()
    {
        if (Interlocked.CompareExchange(ref _isClosed, 1, 0) == 1)
            return;

        Socket.Close();
        Socket = null;

        Peer?.OnRemoved();
        OnSessionClosed?.Invoke(this);
    }

    public void Send(byte[] data)
    {
        lock (_sendQueue)
        {
            _sendQueue.Enqueue(data);
            if (_isSending) return;
            _isSending = true;
        }
        BeginSend();
    }

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

    public void ProcessSend(SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
            BeginSend();
        else
            Close();
    }

    public void OnReceive(byte[] buffer, int offset, int transferred)
    {
        _messageResolver.ReceiveMessage(buffer, offset, transferred, OnMessageCompleted);
    }

    private void OnMessageCompleted(ArraySegment<byte> buffer)
    {
        if (Peer == null) return;
        Packet? packet = Packet.Parse(Peer, buffer);
        if (packet != null)
            Peer.OnMessage(packet);
    }
}
