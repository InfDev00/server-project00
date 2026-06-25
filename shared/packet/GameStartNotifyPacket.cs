// ============================================================
// GameStartNotifyPacket - 게임 시작 통지 (서버 → 방 broadcast)
// 방 정원이 충족되면 해당 방 전원에게 시작을 알린다.
// ============================================================
public class GameStartNotifyPacket : Packet
{
    public short RoomId;
    public int TurnPlayerID;

    // 송신(서버): 시작된 방 번호 기록
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Game_Start_notify;
        WriteShort(RoomId);
        WriteInt(TurnPlayerID);
    }

    // 수신(클라): 방 번호 읽기
    protected override void OnRead()
    {
        RoomId = ReadShort();
        TurnPlayerID = ReadInt();
    }

    public override void Handle() { }
}
