// ============================================================
// RoomEnterAckPacket - 대기방 입장 결과 (서버 → 클라)
// 배정된 방 번호와 현재/필요 인원을 전달한다.
// ============================================================
public class RoomEnterAckPacket : Packet
{
    public short RoomId;
    public int UID;
    public short Current;   // 현재 방 인원
    public short Needed;    // 정원(시작에 필요한 인원)

    // 송신(서버): 방 정보 기록
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Room_Enter_ack;
        WriteInt(UID);
        WriteShort(RoomId);
        WriteShort(Current);
        WriteShort(Needed);
    }

    // 수신(클라): 방 정보 읽기
    protected override void OnRead()
    {
        UID = ReadInt();
        RoomId = ReadShort();
        Current = ReadShort();
        Needed = ReadShort();
    }
}
