using System.Net.Sockets;

public class SocketAsyncEventArgsPool
{
    private Stack<SocketAsyncEventArgs> _pool;

    public int Count => _pool == null ? 0 : _pool.Count;

    public SocketAsyncEventArgsPool(int capacity)
    {
        _pool = new Stack<SocketAsyncEventArgs>(capacity);
    }

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

    public SocketAsyncEventArgs Pop()
    {
        lock (_pool)
            return _pool.Pop();
    }
}