public enum Protocol : short
{
    Login_req = 1,
    Login_ack = 2,
}

public abstract class Packet
{
    public User Owner { get; protected set; }

    protected byte[] _buffer;
    protected int _position;

    public short PacketId { get; protected set; }

    private static readonly Dictionary<Protocol, Func<User, ArraySegment<byte>, Packet>> _parsers = [];

    // 송신용: 빈 버퍼로 패킷 생성 (payload 시작 위치는 헤더 뒤)
    public static T Create<T>(User owner) where T : Packet, new()
    {
        var packet = new T { Owner = owner, _buffer = new byte[1024], _position = MessageResolver.HEADER_SIZE + sizeof(short) };
        packet.OnCreated();
        return packet;
    }

    // 수신용: buffer에서 Protocol ID 읽어 등록된 파서로 생성
    public static Packet? Parse(User owner, ArraySegment<byte> buffer)
    {
        if (buffer.Array is null) return null;
        short id = BitConverter.ToInt16(buffer.Array, buffer.Offset + MessageResolver.HEADER_SIZE);
        var protocol = (Protocol)id;
        return _parsers.TryGetValue(protocol, out var parser) ? parser(owner, buffer) : null;
    }

    // Register 람다에서 수신 패킷 생성 시 사용
    public static T ParseIncoming<T>(User owner, ArraySegment<byte> buffer) where T : Packet, new()
    {
        var packet = new T
        {
            Owner = owner,
            _buffer = buffer.Array!,
            _position = buffer.Offset + MessageResolver.HEADER_SIZE + sizeof(short)
        };
        packet.OnCreated();
        return packet;
    }

    public static void Register()
    {
        _parsers[Protocol.Login_req] = ParseIncoming<LoginReqPacket>;
    }

    // 송신: 길이·PacketId를 헤더에 쓰고 바이트 배열 반환
    public byte[] GetBytes()
    {
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, 0, sizeof(short)), (short)_position);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, sizeof(short), sizeof(short)), PacketId);
        return _buffer[0.._position];
    }

    protected void WriteShort(short value)
    {
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _position, sizeof(short)), value);
        _position += sizeof(short);
    }

    protected short ReadShort()
    {
        short value = BitConverter.ToInt16(_buffer, _position);
        _position += sizeof(short);
        return value;
    }

    protected void WriteString(string value)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        WriteShort((short)bytes.Length);
        bytes.CopyTo(_buffer, _position);
        _position += bytes.Length;
    }

    protected string ReadString()
    {
        short len = ReadShort();
        string str = System.Text.Encoding.UTF8.GetString(_buffer, _position, len);
        _position += len;
        return str;
    }

    public abstract void Handle();

    protected virtual void OnCreated() { }
}
