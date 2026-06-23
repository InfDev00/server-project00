public class LoginAckPacket : Packet
{
    public bool Success { get; set; }

    public override void Handle() { }

    public byte[] Serialize()
    {
        PacketId = (short)Protocol.Login_ack;
        WriteShort(Success ? (short)1 : (short)0);
        return GetBytes();
    }
}
