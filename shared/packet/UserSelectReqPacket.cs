// ============================================================
// UserSelectReqPacket - 플레이어 선택 요청 (클라 → 서버)
// 자기 턴에 Roll(굴리기) 또는 Stop(멈추기)을 선택해 보낸다.
// 누가 보냈는지는 서버가 세션(User.ID)으로 판별하므로 UID는 싣지 않는다.
// ============================================================
public class UserSelectReqPacket : Packet
{
    public bool Stop;   // true=Stop, false=Roll

    protected override void OnWrite()
    {
        PacketId = (short)Protocol.User_Select_req;
        WriteShort((short)(Stop ? 1 : 0));
    }

    protected override void OnRead()
    {
        Stop = ReadShort() == 1;
    }
}
