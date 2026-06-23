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

    public override void Handle()
    {
        Console.WriteLine($"[LoginAck] success={Success}");
    }
}
