// ============================================================
// LoadingCompleteReqPacket - 로딩 완료 보고 (클라 → 서버)
// 페이로드 없음. 서버는 user→room을 알고 있으므로 방을 식별한다.
// 같은 방 전원이 보고하면(준비 배리어) 서버가 GameStart를 broadcast한다.
// ============================================================
public class LoadingCompleteReqPacket : Packet
{
    // 송신(클라): 식별자만 기록
    protected override void OnWrite()
    {
        PacketId = (short)Protocol.Loading_complete_req;
    }

    // 수신(서버): 읽을 필드 없음
    protected override void OnRead() { }

    // 서버 처리는 User.OnMessage에서 라우팅하므로 비움
    public override void Handle() { }
}
