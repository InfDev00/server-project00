// ============================================================
// RoomEnterReqPacket - 대기방 입장 요청 (클라 → 서버)
// 페이로드 없음. 서버는 이 패킷을 받으면 매칭 큐(대기방)에 넣는다.
// ============================================================
public class RoomEnterReqPacket : Packet
{
    // 송신(클라): 식별자만 기록
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Room_Enter_req;
    }

    // 수신(서버): 읽을 필드 없음
    protected override void OnRead() { }

    // 서버 처리는 User.OnMessage에서 라우팅하므로 비움
    public override void Handle() { }
}
