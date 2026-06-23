public class LoginReqPacket : Packet
{
    public string Username { get; private set; } = string.Empty;

    protected override void OnCreated()
    {
        Username = ReadString();
    }

    public override void Handle()
    {
        Console.WriteLine($"[LoginReq] {Username}");

        var ack = Packet.Create<LoginAckPacket>(Owner);
        ack.Success = true;
        Owner.Send(ack.Serialize());
    }
}
