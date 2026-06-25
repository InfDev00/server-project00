// ============================================================
// GameStartNotifyPacket - 게임 시작 통지 (서버 → 방 broadcast)
// 전원 로딩 완료 시 방 전원에게 시작과 선턴 플레이어를 알린다.
// 클라는 TurnPlayerID == 내 ID 인지로 내 턴 여부를 판단한다.
// ============================================================
public class GameStartNotifyPacket : Packet
{
    public short RoomId;
    public int TurnPlayerID;     // 선턴(첫 턴) 플레이어의 ID

    // 송신(서버): 방 번호 + 선턴 플레이어 ID 기록
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Game_Start_notify;
        WriteShort(RoomId);
        WriteInt(TurnPlayerID);
    }

    // 수신(클라): 방 번호 + 선턴 플레이어 ID 읽기
    protected override void OnRead()
    {
        RoomId = ReadShort();
        TurnPlayerID = ReadInt();
    }

    public override void Handle() { }
}
