// ============================================================
// LoginReqPacket - 로그인 요청 패킷 (클라 → 서버)
// 사용자 이름을 실어 보내고, 서버는 Handle에서 LoginAck로 응답한다.
// ============================================================
public class LoginReqPacket : Packet
{
    public string Username { get; set; } = string.Empty;

    // 송신(클라): username을 버퍼에 쓴다
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Login_req;
        WriteString(Username);
    }

    // 수신(서버): 버퍼에서 username을 읽는다
    protected override void OnRead()
    {
        Username = ReadString();
    }

    public override void Handle()
    {
        Console.WriteLine($"  Protocol : {Protocol.Login_req} (id={( short)Protocol.Login_req})");
        Console.WriteLine($"  Username : {Username}");

        var ack = Packet.Create<LoginAckPacket>(Owner);
        ack.Success = true;
        Owner.Send(ack);
    }
}
