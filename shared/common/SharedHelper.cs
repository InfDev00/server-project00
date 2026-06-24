using System.Net.Sockets;

public static class SharedHelper
{
    public static bool TryGetSession(this SocketAsyncEventArgs args, out Session? session)
    {
        if (args.UserToken is Session _session)
        {
            session = _session;
            return true;
        }

        session = default;
        return false;
    }

    public static void ReceiveAsyncEx(this Socket socket, SocketAsyncEventArgs args, Action<SocketAsyncEventArgs> onCompleted)
    {
        if (!socket.ReceiveAsync(args))
        {
            onCompleted?.Invoke(args);
        }
    }

    public static void SendAsyncEx(this Socket socket, SocketAsyncEventArgs args, Action<SocketAsyncEventArgs> onCompleted)
    {
        if (!socket.SendAsync(args))
        {
            onCompleted?.Invoke(args);
        }
    }

    public static void ConnectAsyncEx(this Socket socket, SocketAsyncEventArgs args, Action<object?, SocketAsyncEventArgs> onCompleted)
    {
        if (!socket.ConnectAsync(args))
        {
            onCompleted?.Invoke(null, args);
        }
    }
}
