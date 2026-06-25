// ============================================================
// LoginReqPacket - 로그인 요청 패킷 (클라 → 서버)
// 사용자 이름을 실어 보낸다. 서버 처리는 User.OnMessage가 담당한다.
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

    // 서버 처리는 User.OnMessage에서 라우팅하므로 비움
    public override void Handle() { }
}
