// TCP는 "메시지 경계"가 없다. 보낸 쪽이 한 번에 보낸 패킷이라도 받는 쪽에서는
// 여러 번에 쪼개져 오거나(분할), 여러 패킷이 한 번에 붙어 올 수 있다(병합).
// 그래서 "앞 2바이트 = 전체 길이" 헤더를 붙여, 그 길이만큼 모일 때까지 누적했다가
// 한 패킷이 완성되면 콜백으로 넘긴다. 이 누적·재조립을 담당하는 게 MessageResolver다.
//
// 수신 콜백이 올 때마다 OnReceive 가 호출되며, 받은 조각을 message_buffer_ 에
// 이어 붙이다가 한 통이 완성되면 callback 을 부른다.
public class MessageResolver
{
    int message_size_;                       // 현재 조립 중인 패킷의 전체 길이(헤더 포함)
    byte[] message_buffer_ = new byte[1024]; // 한 패킷을 누적해 담는 작업 버퍼
    int current_position_;                   // message_buffer_ 에 지금까지 채운 양
    int remain_bytes_;                       // 이번 수신분에서 아직 처리 못 한 바이트 수

    public delegate void CompletedMessageCallback(ArraySegment<byte> data);

    // 원본 버퍼(buffer)에서 position_to_read 위치까지 채우도록 가능한 만큼만 복사한다.
    // 다 채웠으면 true, 아직 모자라면(다음 수신 필요) false.
    bool ReadUntil(byte[] buffer, ref int src_position, int position_to_read)
    {
        // 목표까지 남은 양과, 이번 수신분에 남은 양 중 작은 만큼만 복사
        int copy_size = Math.Min(position_to_read - current_position_, remain_bytes_);

        Array.Copy(buffer, src_position, message_buffer_, current_position_, copy_size);

        src_position += copy_size;       // 원본에서 읽은 위치 전진
        current_position_ += copy_size;  // 작업 버퍼에 채운 양 증가
        remain_bytes_ -= copy_size;      // 이번 수신분 남은 양 감소

        // 목표 위치까지 못 채웠으면 아직 미완성
        if (current_position_ < position_to_read)
            return false;

        return true;
    }

    // 소켓이 수신한 raw 바이트(buffer의 offset부터 transferred만큼)를 넘겨받아 처리한다.
    public void OnReceive(byte[] buffer, int offset, int transferred, CompletedMessageCallback callback)
    {
        remain_bytes_ = transferred;
        int src_position = offset;

        // 이번에 받은 바이트를 다 소진할 때까지 반복(여러 패킷이 붙어 왔을 수 있으므로)
        while (remain_bytes_ > 0)
        {
            bool completed = false;

            // 1) 아직 헤더(앞 2바이트)를 다 못 읽었으면 먼저 헤더부터 채운다.
            if (current_position_ < 2)
            {
                completed = ReadUntil(buffer, ref src_position, 2);
                if (completed == false)
                    return; // 헤더도 다 못 모았다 → 다음 수신 대기

                // 헤더 2바이트를 읽었으니 전체 길이를 구한다.
                message_size_ = GetTotalMessageSize();

                if (message_size_ <= 0)
                {
                    ClearBuffer();
                    return; // 비정상 길이 → 버리고 종료
                }

                if (remain_bytes_ <= 0)
                    return; // 헤더만 오고 본문은 아직 → 다음 수신 대기
            }

            // 2) 본문까지 포함해 message_size_ 만큼 다 채웠으면 한 패킷 완성.
            completed = ReadUntil(buffer, ref src_position, message_size_);
            if (completed == true)
            {
                // 완성된 패킷을 복제해 콜백에 전달(작업 버퍼는 곧 재사용되므로)
                byte[] clone = new byte[message_size_];
                Array.Copy(message_buffer_, clone, message_size_);
                ClearBuffer(); // 다음 패킷을 위해 초기화
                callback?.Invoke(new ArraySegment<byte>(clone, 0, message_size_));
            }
        }
    }

    // 헤더(앞 2바이트)를 읽어 전체 메시지 길이를 구한다.
    private int GetTotalMessageSize()
    {
        return BitConverter.ToInt16(message_buffer_, 0);
    }

    // 한 패킷 처리를 마쳤거나 연결을 정리할 때 작업 버퍼를 초기 상태로 되돌린다.
    public void ClearBuffer()
    {
        Array.Clear(message_buffer_, 0, message_buffer_.Length);

        current_position_ = 0;
        message_size_ = 0;
    }
}
