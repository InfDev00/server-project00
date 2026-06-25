using System.Net.Sockets;

// ============================================================
// SharedHelper - 소켓/SAEA 관련 확장 메서드 모음
// *Async가 동기 완료(false 반환)되면 Completed 이벤트가 안 뜨므로,
// 그 경우 콜백을 직접 호출해 동기·비동기 경로를 통일시킨다.
// ============================================================
public static class SharedHelper
{
    // SAEA.UserToken에 담긴 Session을 안전하게 꺼낸다
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

    // 비동기 수신. 동기 완료 시 콜백 직접 호출
    public static void ReceiveAsyncEx(this Socket socket, SocketAsyncEventArgs args, Action<SocketAsyncEventArgs> onCompleted)
    {
        if (!socket.ReceiveAsync(args))
        {
            onCompleted?.Invoke(args);
        }
    }

    // 비동기 송신. 동기 완료 시 콜백 직접 호출
    public static void SendAsyncEx(this Socket socket, SocketAsyncEventArgs args, Action<SocketAsyncEventArgs> onCompleted)
    {
        if (!socket.SendAsync(args))
        {
            onCompleted?.Invoke(args);
        }
    }

    // 비동기 연결. 동기 완료 시 콜백 직접 호출
    public static void ConnectAsyncEx(this Socket socket, SocketAsyncEventArgs args, Action<object?, SocketAsyncEventArgs> onCompleted)
    {
        if (!socket.ConnectAsync(args))
        {
            onCompleted?.Invoke(null, args);
        }
    }
}
