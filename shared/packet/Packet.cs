// 패킷 종류 식별자 (헤더 뒤 2바이트에 기록되는 ID)
public enum Protocol : short
{
    Login_req = 1,
    Login_ack = 2,
}

// ============================================================
// Packet - 모든 패킷의 추상 베이스 + 직렬화/역직렬화 골격
// 와이어 포맷: [2바이트 길이][2바이트 Protocol ID][payload].
// 송신은 Create→OnWrite→Pack, 수신은 Parse→OnRead 흐름을 탄다.
// 새 패킷은 Register()에 등록해야 수신 측에서 인식된다.
// ============================================================
public abstract class Packet
{
    public IPeer Owner { get; protected set; }   // 이 패킷을 주고받는 상대(세션)

    protected byte[] _buffer;       // 직렬화 버퍼
    protected int _position;        // 현재 읽기/쓰기 커서 위치

    public short PacketId { get; protected set; }

    // Protocol ID → 수신 패킷 생성 함수 매핑 테이블
    private static readonly Dictionary<Protocol, Func<IPeer, ArraySegment<byte>, Packet>> _parsers = [];

    // 송신용: 빈 버퍼로 패킷 생성 (payload 시작 위치는 헤더 뒤)
    public static T Create<T>(IPeer owner) where T : Packet, new()
    {
        var packet = new T { Owner = owner, _buffer = new byte[1024], _position = MessageResolver.HEADER_SIZE + sizeof(short) };
        return packet;
    }

    // 수신용: buffer에서 Protocol ID 읽어 등록된 파서로 생성
    public static Packet? Parse(IPeer owner, ArraySegment<byte> buffer)
    {
        if (buffer.Array is null) return null;
        short id = BitConverter.ToInt16(buffer.Array, buffer.Offset + MessageResolver.HEADER_SIZE);
        var protocol = (Protocol)id;
        return _parsers.TryGetValue(protocol, out var parser) ? parser(owner, buffer) : null;
    }

    // Register 람다에서 수신 패킷 생성 시 사용
    public static T ParseIncoming<T>(IPeer owner, ArraySegment<byte> buffer) where T : Packet, new()
    {
        var packet = new T
        {
            Owner = owner,
            _buffer = buffer.Array!,
            _position = buffer.Offset + MessageResolver.HEADER_SIZE + sizeof(short)
        };
        packet.OnRead();
        return packet;
    }

    // 앱 시작 시 1회 호출 — 모든 수신 패킷 파서를 등록 (새 패킷은 여기 추가)
    public static void Register()
    {
        _parsers[Protocol.Login_req] = ParseIncoming<LoginReqPacket>;
        _parsers[Protocol.Login_ack] = ParseIncoming<LoginAckPacket>;
    }

    // 송신: 하위 클래스가 payload를 쓰고(OnWrite) 헤더까지 채워 바이트 반환
    public byte[] Pack()
    {
        OnWrite();
        return GetBytes();
    }

    // 송신: 길이·PacketId를 헤더에 쓰고 바이트 배열 반환
    public byte[] GetBytes()
    {
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, 0, sizeof(short)), (short)_position);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, sizeof(short), sizeof(short)), PacketId);
        return _buffer[0.._position];
    }

    // short 1개 쓰고 커서 전진
    protected void WriteShort(short value)
    {
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _position, sizeof(short)), value);
        _position += sizeof(short);
    }

    // short 1개 읽고 커서 전진
    protected short ReadShort()
    {
        short value = BitConverter.ToInt16(_buffer, _position);
        _position += sizeof(short);
        return value;
    }

    // 문자열을 [길이(short)][UTF-8 바이트] 형태로 기록
    protected void WriteString(string value)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        WriteShort((short)bytes.Length);
        bytes.CopyTo(_buffer, _position);
        _position += bytes.Length;
    }

    // [길이(short)][UTF-8 바이트] 형태의 문자열 읽기
    protected string ReadString()
    {
        short len = ReadShort();
        string str = System.Text.Encoding.UTF8.GetString(_buffer, _position, len);
        _position += len;
        return str;
    }

    // 수신 측 처리 로직 (하위 클래스가 구현)
    public abstract void Handle();

    // 수신: 버퍼에서 필드 읽기 (하위 클래스가 override)
    protected virtual void OnRead() { }

    // 송신: 버퍼에 필드 쓰기 (하위 클래스가 override)
    protected virtual void OnWrite() { }
}
