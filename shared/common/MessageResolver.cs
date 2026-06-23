public class MessageResolver
{
    public static readonly int HEADER_SIZE = 2;

    readonly byte[] _messageBuffer;

    int _remainBytes = 0;
    int _currentPosition = 0;
    int _messageSize = 0;

    public MessageResolver(int bufferSize)
    {
        _messageBuffer = new byte[bufferSize];
    }

    private bool ReadUntil(byte[] buffer, ref int srcPostion, int positionToRead)
    {
        int copySize = Math.Min(positionToRead - _currentPosition, _remainBytes);

        Array.Copy(buffer, srcPostion, _messageBuffer, _currentPosition, copySize);

        srcPostion += copySize;
        _currentPosition += copySize;
        _remainBytes -= copySize;

        return _currentPosition >= positionToRead;
    }

    public void ReceiveMessage(byte[] buffer, int offset, int transferred, Action<ArraySegment<byte>> callback)
    {
        _remainBytes = transferred;
        int src_offset = offset;

        while (_remainBytes > 0)
        {
            bool completed = false;
            if (_currentPosition < HEADER_SIZE)
            {
                completed = ReadUntil(buffer, ref src_offset, HEADER_SIZE);
                if (completed == false)
                    return;

                _messageSize = BitConverter.ToInt16(_messageBuffer, 0);
                if (_messageSize <= 0)
                {
                    ClearBuffer();
                    return;
                }

                if (_remainBytes < 0)
                    return;
            }

            completed = ReadUntil(buffer, ref src_offset, _messageSize);

            if (completed == true)
            {
                byte[] clone = new byte[_messageSize];
                Array.Copy(_messageBuffer, clone, _messageSize);
                ClearBuffer();
                callback?.Invoke(new ArraySegment<byte>(clone, 0, _messageSize));
            }
        }
    }

    public void ClearBuffer()
    {
        Array.Clear(_messageBuffer, 0, _messageBuffer.Length);

        _currentPosition = 0;
        _messageSize = 0;
    }
}