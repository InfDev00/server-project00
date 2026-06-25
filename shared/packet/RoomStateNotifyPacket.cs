// ============================================================
// RoomStateNotifyPacket - 대기방 인원 변동 통지 (서버 → 방 broadcast)
// 같은 방의 누군가 입장/이탈할 때 현재/필요 인원을 갱신해 알린다.
// ============================================================
public class RoomStateNotifyPacket : Packet
{
    public short Current;
    public short Needed;

    // 송신(서버): 인원 기록
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Room_State_notify;
        WriteShort(Current);
        WriteShort(Needed);
    }

    // 수신(클라): 인원 읽기
    protected override void OnRead()
    {
        Current = ReadShort();
        Needed = ReadShort();
    }

    public override void Handle() { }
}
