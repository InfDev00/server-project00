using System.Net.Sockets;

public class NetworkService
{
    private SocketAsyncEventArgsPool _receiveEventArgsPool;
    private SocketAsyncEventArgsPool _sendEventArgsPool;

    private BufferManager _bufferManager;
    int _bufferSize;

    public void Initialize() => Initialize(10000, 1024);
    public void Initialize(int maxConnection, int bufferSize)
    {
        Packet.Register();

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

    private void OnNewClientEnter(Socket clientSocket, object token)
    {
        SocketAsyncEventArgs receiveArgs = _receiveEventArgsPool.Pop();
        SocketAsyncEventArgs sendArgs = _sendEventArgsPool.Pop();

        User user = new User(_bufferSize);
        user.OnSessionClosed += OnSessionClosed;

        receiveArgs.UserToken = user;
        sendArgs.UserToken = user;

        user.OnConnected();

        BeginReceive(clientSocket, receiveArgs, sendArgs);
    }

    private void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.TryGetUser(out User user))
        {
            user.ProcessSend(args);
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
        if (receiveArgs.TryGetUser(out User user))
        {
            user.ReceiveEventArgs = receiveArgs;
            user.SendEventArgs = sendArgs;

            user.Socket = socket;

            socket.ReceiveAsyncEx(receiveArgs, ProcessReceive);
        }
    }

    private void ProcessReceive(SocketAsyncEventArgs args)
    {
        if (args.TryGetUser(out User user))
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                user.OnReceive(args.Buffer, args.Offset, args.BytesTransferred);

                user.Socket.ReceiveAsyncEx(args, ProcessReceive);
            }
            else
            {
                user.Close();
            }
        }
    }

    private void OnSessionClosed(User user)
    {
        _receiveEventArgsPool?.Push(user.ReceiveEventArgs);
        _sendEventArgsPool?.Push(user.SendEventArgs);

        user.ReceiveEventArgs = null;
        user.SendEventArgs = null;
    }
}