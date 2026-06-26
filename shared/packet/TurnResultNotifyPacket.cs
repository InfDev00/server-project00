// ============================================================
// TurnResultNotifyPacket - 턴 결과 통지 (서버 → 방 broadcast)
// 한 플레이어의 행동 결과를 알린다: 굴린 값, 그 플레이어의 누적 합,
// 게임오버 여부, 다음 턴 플레이어, 그리고 게임오버 시 승자.
// 클라는 UID == 내 ID 로 누구 손패를 갱신할지, TurnPlayerID == 내 ID 로
// 내 턴 여부를, WinnerID == 내 ID 로 승패를 판단한다.
// ============================================================
public class TurnResultNotifyPacket : Packet
{
    public int UID;             // 이번에 행동한 플레이어
    public int Result;          // 굴려 나온 값 (Stop이면 0)
    public int Total;           // 그 플레이어의 누적 합
    public short IsGameOver;    // 1=게임오버, 0=계속
    public int TurnPlayerID;    // 다음 턴 플레이어 ID
    public int WinnerID;        // 게임오버 시 승자 ID (-1=무승부, IsGameOver=1일 때만 유효)

    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Turn_Result_notify;
        WriteInt(UID);
        WriteInt(Result);
        WriteInt(Total);
        WriteShort(IsGameOver);
        WriteInt(TurnPlayerID);
        WriteInt(WinnerID);
    }

    protected override void OnRead()
    {
        UID = ReadInt();
        Result = ReadInt();
        Total = ReadInt();
        IsGameOver = ReadShort();
        TurnPlayerID = ReadInt();
        WinnerID = ReadInt();
    }
}
