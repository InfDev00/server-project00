using System.Net.Sockets;

// ============================================================
// BufferManager - 하나의 큰 버퍼를 여러 SAEA가 나눠 쓰도록 관리
// 큰 배열을 미리 잡아두고 구간(offset)만 나눠줘 메모리 단편화와
// 핀(pinning) 비용을 줄인다. 반납된 구간은 재사용한다.
// ============================================================
public class BufferManager
{
    byte[] _buffer;                 // 전체를 담는 단일 연속 버퍼
    Stack<int> _freeIndexPool;      // 반납된 구간 offset 재사용 풀
    int _currentIndex;              // 아직 할당 안 한 다음 구간 시작 위치
    int _bufferSize;                // SAEA 1개당 구간 크기

    public BufferManager(int totalSize, int bufferSize)
    {
        _buffer = new byte[totalSize];
        _freeIndexPool = new Stack<int>();
        _currentIndex = 0;
        _bufferSize = bufferSize;
    }

    // SAEA에 버퍼 구간을 할당. 공간 부족 시 false
    public bool SetBuffer(SocketAsyncEventArgs args)
    {
        // 반납된 구간이 있으면 우선 재사용
        if (_freeIndexPool.Count > 0)
        {
            args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
        }
        else
        {
            // 없으면 새 구간을 잘라줌 (끝까지 찼으면 실패)
            if (_buffer.Length - _bufferSize < _currentIndex)
                return false;

            args.SetBuffer(_buffer, _currentIndex, _bufferSize);
            _currentIndex += _bufferSize;
        }

        return true;
    }

    // 사용하던 구간을 풀에 반납
    public void FreeBuffer(SocketAsyncEventArgs args)
    {
        _freeIndexPool.Push(args.Offset);
        args.SetBuffer(null, 0, 0);
    }
}