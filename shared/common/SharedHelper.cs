using System.Net.Sockets;

public static class SharedHelper
{
    public static bool TryGetUser(this SocketAsyncEventArgs args, out User user)
    {
        if (args.UserToken is User _user)
        {
            user = _user;
            return true;
        }

        user = default;
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
}