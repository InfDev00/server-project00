using System.Net;
using System.Net.Sockets;

public class Listener
{
    SocketAsyncEventArgs _acceptArgs;
    Socket _listenSocket;

    AutoResetEvent _flowControlEvent;

    public delegate void OnClientEnterHandler(Socket clientSocket, object token);
    public OnClientEnterHandler ClientEnterCallback;

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

    private void DoListen()
    {
        _flowControlEvent = new AutoResetEvent(false);

        while (true)
        {
            _acceptArgs.AcceptSocket = null;

            bool pending = _listenSocket.AcceptAsync(_acceptArgs);
            if (pending == false)
                OnAcceptCompleted(null, _acceptArgs);

            _flowControlEvent.WaitOne();
        }
    }

    private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
    {
        if (args.SocketError == SocketError.Success)
        {
            Socket clientSocket = args.AcceptSocket;
            clientSocket.NoDelay = true;

            ClientEnterCallback?.Invoke(clientSocket, args);
        }
        else
        {
            Console.WriteLine("Fail to Accept Client. " + args.SocketError);
        }

        _flowControlEvent.Set();
    }
}