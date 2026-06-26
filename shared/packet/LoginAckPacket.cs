// ============================================================
// LoginAckPacket - 로그인 응답 패킷 (서버 → 클라)
// 로그인 성공 여부(Success)를 전달한다.
// ============================================================
public class LoginAckPacket : Packet
{
    public bool Success { get; set; }

    // 송신(서버): 성공 여부를 버퍼에 쓴다
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Login_ack;
        WriteShort(Success ? (short)1 : (short)0);
    }

    // 수신(클라): 버퍼에서 성공 여부를 읽는다
    protected override void OnRead()
    {
        Success = ReadShort() == 1;
    }
}
