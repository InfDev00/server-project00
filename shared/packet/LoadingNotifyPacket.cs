// ============================================================
// LoadingNotifyPacket - 로딩 시작 통지 (서버 → 방 broadcast)
// 방 정원이 충족되면 전원에게 "이제 로딩하라"고 알린다.
// 클라는 로딩 완료 후 LoadingCompleteReq로 응답한다.
// ============================================================
public class LoadingNotifyPacket : Packet
{
    public short RoomId;

    // 송신(서버): 로딩할 방 번호 기록
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Loading_notify;
        WriteShort(RoomId);
    }

    // 수신(클라): 방 번호 읽기
    protected override void OnRead()
    {
        RoomId = ReadShort();
    }

    public override void Handle() { }
}
