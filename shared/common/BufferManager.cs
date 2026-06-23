using System.Net.Sockets;

public class BufferManager
{
    byte[] _buffer;
    Stack<int> _freeIndexPool;
    int _currentIndex;
    int _bufferSize;

    public BufferManager(int totalSize, int bufferSize)
    {
        _buffer = new byte[totalSize];
        _freeIndexPool = new Stack<int>();
        _currentIndex = 0;
        _bufferSize = bufferSize;
    }

    public bool SetBuffer(SocketAsyncEventArgs args)
    {
        if (_freeIndexPool.Count > 0)
        {
            args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
        }
        else
        {
            if (_buffer.Length - _bufferSize < _currentIndex)
                return false;

            args.SetBuffer(_buffer, _currentIndex, _bufferSize);
            _currentIndex += _bufferSize;
        }

        return true;
    }

    public void FreeBuffer(SocketAsyncEventArgs args)
    {
        _freeIndexPool.Push(args.Offset);
        args.SetBuffer(null, 0, 0);
    }
}