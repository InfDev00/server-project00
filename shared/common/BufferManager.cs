using System.Net.Sockets;

namespace Shared.Common;

// 모든 소켓이 쓸 수신 버퍼를 "거대한 바이트 배열 하나"로 미리 잡아두고,
// 각 SocketAsyncEventArgs(SAEA)에 그 배열의 한 구간(offset~size)만 잘라서 배정한다.
//
// 왜 이렇게 하나?
//  - 연결마다 buffer = new byte[...] 로 잡으면 GC 대상이 늘고, 비동기 I/O가
//    버퍼를 고정(pinning)하면서 힙이 단편화된다.
//  - 큰 배열 하나를 잡아 나눠 쓰면 할당이 1회로 끝나고 고정 구간도 한 덩어리라
//    GC 압박·단편화가 줄어든다.
internal class BufferManager
{
    int num_bytes_;            // 전체 버퍼 크기(= 최대 동접 × 버퍼 크기)
    byte[] buffer_;            // 실제로 한 번에 잡는 거대한 바이트 배열
    Stack<int> free_index_pool_; // 반납된(다시 쓸 수 있는) 구간의 시작 offset 모음
    int current_index_;        // 아직 한 번도 나눠주지 않은 영역의 현재 위치
    int buffer_size_;          // SAEA 하나에 배정할 구간 크기

    public BufferManager(int total_bytes, int buffer_size)
    {
        num_bytes_ = total_bytes;
        current_index_ = 0;
        buffer_size_ = buffer_size;
        free_index_pool_ = new Stack<int>();

        buffer_ = new byte[num_bytes_]; // 여기서 단 한 번 크게 할당
    }

    // 인자로 받은 SAEA에 버퍼 구간을 하나 배정한다. 성공하면 true.
    public bool SetBuffer(SocketAsyncEventArgs args)
    {
        if (free_index_pool_.Count > 0)
        {
            // 반납된 구간이 있으면 그것을 재사용
            args.SetBuffer(buffer_, free_index_pool_.Pop(), buffer_size_);
        }
        else
        {
            // 남은 공간이 모자라면 실패(더 이상 배정 불가)
            if (num_bytes_ - buffer_size_ < current_index_)
                return false;

            // 아직 안 쓴 영역에서 buffer_size_ 만큼 잘라 배정하고 커서를 전진
            args.SetBuffer(buffer_, current_index_, buffer_size_);
            current_index_ += buffer_size_;
        }

        return true;
    }

    // 연결이 끝난 SAEA의 구간을 반납한다. 그 offset은 free 풀에 들어가 재사용된다.
    public void FreeBuffer(SocketAsyncEventArgs args)
    {
        free_index_pool_.Push(args.Offset);
        args.SetBuffer(null, 0, 0);
    }
}
