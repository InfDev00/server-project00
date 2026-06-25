// ============================================================
// MessageResolver - TCP 스트림을 메시지 단위로 복원(프레이밍)
// 메시지 = [2바이트 길이 헤더][본문]. TCP는 경계가 없어 한 번에
// 일부만 오거나 여러 개가 붙어 올 수 있으므로, 길이만큼 모일 때마다
// 완성된 메시지 하나씩 콜백으로 넘긴다.
// ============================================================
public class MessageResolver
{
    public static readonly int HEADER_SIZE = 2;   // 길이 헤더 크기(short)

    readonly byte[] _messageBuffer;     // 조립 중인 메시지를 모으는 버퍼

    int _remainBytes = 0;               // 이번 수신분에서 아직 안 읽은 바이트
    int _currentPosition = 0;           // 조립 버퍼에 채워진 위치
    int _messageSize = 0;               // 헤더에서 읽은 이번 메시지 전체 크기

    public MessageResolver(int bufferSize)
    {
        _messageBuffer = new byte[bufferSize];
    }

    // 수신 버퍼에서 positionToRead 위치까지 조립 버퍼로 복사. 도달 시 true
    private bool ReadUntil(byte[] buffer, ref int srcPostion, int positionToRead)
    {
        // 목표까지 남은 양과 실제 남은 수신분 중 작은 만큼만 복사
        int copySize = Math.Min(positionToRead - _currentPosition, _remainBytes);

        Array.Copy(buffer, srcPostion, _messageBuffer, _currentPosition, copySize);

        srcPostion += copySize;
        _currentPosition += copySize;
        _remainBytes -= copySize;

        return _currentPosition >= positionToRead;
    }

    // 수신분(transferred 바이트)을 받아 완성되는 메시지마다 callback 호출
    public void ReceiveMessage(byte[] buffer, int offset, int transferred, Action<ArraySegment<byte>> callback)
    {
        _remainBytes = transferred;
        int src_offset = offset;

        // 남은 수신분이 있는 동안 반복 (한 번에 여러 메시지가 붙어 올 수 있음)
        while (_remainBytes > 0)
        {
            bool completed = false;

            // 1) 아직 헤더를 못 읽었으면 헤더부터 채운다
            if (_currentPosition < HEADER_SIZE)
            {
                completed = ReadUntil(buffer, ref src_offset, HEADER_SIZE);
                if (completed == false)
                    return;     // 헤더도 다 못 모았으면 다음 수신을 기다림

                _messageSize = BitConverter.ToInt16(_messageBuffer, 0);
                if (_messageSize <= 0)
                {
                    ClearBuffer();
                    return;
                }

                if (_remainBytes < 0)
                    return;
            }

            // 2) 본문을 메시지 크기까지 채운다
            completed = ReadUntil(buffer, ref src_offset, _messageSize);

            // 3) 한 메시지 완성 → 복사본을 만들어 콜백에 전달하고 버퍼 초기화
            if (completed == true)
            {
                byte[] clone = new byte[_messageSize];
                Array.Copy(_messageBuffer, clone, _messageSize);
                ClearBuffer();
                callback?.Invoke(new ArraySegment<byte>(clone, 0, _messageSize));
            }
        }
    }

    // 조립 상태 초기화 (다음 메시지 수신 준비)
    public void ClearBuffer()
    {
        Array.Clear(_messageBuffer, 0, _messageBuffer.Length);

        _currentPosition = 0;
        _messageSize = 0;
    }
}