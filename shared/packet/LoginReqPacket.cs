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
        Console.WriteLine($"[LoginReq] {Username}");

        var ack = Packet.Create<LoginAckPacket>(Owner);
        ack.Success = true;
        Owner.Send(ack.Pack());
    }
}
