using System.Net;
using System.Net.Sockets;

public class NetworkService
{
    private SocketAsyncEventArgsPool _receiveEventArgsPool;
    private SocketAsyncEventArgsPool _sendEventArgsPool;

    private BufferManager _bufferManager;
    int _bufferSize;

    public Action<Session> OnSessionCreated;

    public NetworkService()
    {
        Packet.Register();
        OnSessionCreated = null;
    }

    public void Initialize() => Initialize(10000, 1024);
    public void Initialize(int maxConnection, int bufferSize)
    {
        _bufferSize = bufferSize;

        int preAllocCount = 1;

        _bufferManager = new BufferManager(maxConnection * bufferSize * preAllocCount, bufferSize);
        _receiveEventArgsPool = new SocketAsyncEventArgsPool(maxConnection);
        _sendEventArgsPool = new SocketAsyncEventArgsPool(maxConnection);

        for (int i = 0; i < maxConnection; ++i)
        {
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
                args.UserToken = null;

                _bufferManager.SetBuffer(args);
                _receiveEventArgsPool.Push(args);
            }

            {
                var args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
                args.UserToken = null;

                args.SetBuffer(null, 0, 0);
                _sendEventArgsPool.Push(args);
            }
        }
    }

    public void Listen(string host, int port, int backlog)
    {
        Listener clientListener = new Listener();
        clientListener.ClientEnterCallback += OnNewClientEnter;
        clientListener.Start(host, port, backlog);
    }

    public void Connect(IPEndPoint remoteEndpoint)
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket.NoDelay = true;

        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
        args.Completed += OnConnectCompleted;
        args.RemoteEndPoint = remoteEndpoint;

        clientSocket.ConnectAsyncEx(args, OnConnectCompleted);
    }

    private void OnConnectCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            Session session = new Session(1024);

            session.OnSessionClosed += OnSessionClosed;

            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            receiveArgs.UserToken = session;
            receiveArgs.SetBuffer(new byte[1024], 0, 1024);

            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            sendArgs.UserToken = session;
            sendArgs.SetBuffer(null, 0, 0);

            OnSessionCreated?.Invoke(session);

            BeginReceive(args.ConnectSocket, receiveArgs, sendArgs);
        }
    }

    private void OnNewClientEnter(Socket clientSocket, object token)
    {
        SocketAsyncEventArgs receiveArgs = _receiveEventArgsPool.Pop();
        SocketAsyncEventArgs sendArgs = _sendEventArgsPool.Pop();

        Session session = new(_bufferSize);
        session.OnSessionClosed += OnSessionClosed;

        receiveArgs.UserToken = session;
        sendArgs.UserToken = session;

        OnSessionCreated?.Invoke(session);

        BeginReceive(clientSocket, receiveArgs, sendArgs);
    }

    private void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.TryGetSession(out Session? session))
        {
            session!.ProcessSend(args);
        }
    }

    private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.LastOperation == SocketAsyncOperation.Receive)
        {
            ProcessReceive(args);
        }
    }

    private void BeginReceive(Socket socket, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
    {
        if (receiveArgs.TryGetSession(out Session? session))
        {
            session!.ReceiveEventArgs = receiveArgs;
            session.SendEventArgs = sendArgs;

            session.Socket = socket;

            socket.ReceiveAsyncEx(receiveArgs, ProcessReceive);
        }
    }

    private void ProcessReceive(SocketAsyncEventArgs args)
    {
        if (args.TryGetSession(out Session? session))
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                session!.OnReceive(args.Buffer, args.Offset, args.BytesTransferred);

                session.Socket.ReceiveAsyncEx(args, ProcessReceive);
            }
            else
            {
                session!.Close();
            }
        }
    }

    private void OnSessionClosed(Session session)
    {
        _receiveEventArgsPool?.Push(session.ReceiveEventArgs);
        _sendEventArgsPool?.Push(session.SendEventArgs);

        session.ReceiveEventArgs = null;
        session.SendEventArgs = null;
    }
}
