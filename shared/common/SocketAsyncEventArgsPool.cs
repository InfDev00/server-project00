using System.Net.Sockets;

// ============================================================
// SocketAsyncEventArgsPool - SAEA 객체 재사용 풀
// 비싼 SAEA를 매번 만들지 않고 풀에서 빌려 쓰고 반납해 GC를 줄인다.
// Push/Pop은 lock으로 보호되어 멀티스레드에서 안전하다.
// ============================================================
public class SocketAsyncEventArgsPool
{
    private Stack<SocketAsyncEventArgs> _pool;

    public int Count => _pool == null ? 0 : _pool.Count;

    public SocketAsyncEventArgsPool(int capacity)
    {
        _pool = new Stack<SocketAsyncEventArgs>(capacity);
    }

    // 사용 끝난 SAEA를 풀에 반납 (중복 반납은 예외)
    public void Push(SocketAsyncEventArgs args)
    {
        if (args is null) throw new ArgumentNullException(nameof(args));

        lock (_pool)
        {
            if (_pool.Contains(args))
            {
                throw new Exception("args already contains");
            }
            _pool.Push(args);
        }
    }

    // 풀에서 SAEA 하나를 꺼냄
    public SocketAsyncEventArgs Pop()
    {
        lock (_pool)
            return _pool.Pop();
    }
}