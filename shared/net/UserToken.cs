using System.Net.Sockets;

namespace Shared.Net;

// 접속 1건 = 세션 1개를 나타낸다(클라이언트 한 명과 1:1 대응).
// 자기 소켓과 송/수신 SAEA를 들고, 받은 바이트를 MessageResolver로 패킷 단위로 잘라
// 콘텐츠(게임 로직)에 올려주는 "네트워크 세션" 객체.
public class UserToken
{
    public Socket Socket; // 이 세션의 클라이언트 소켓

    // 이 세션이 쓰는 송/수신 SAEA (NetworkService가 풀에서 꺼내 SetEventArgs로 넘겨준다).
    public SocketAsyncEventArgs receive_event_args { get; private set; }
    public SocketAsyncEventArgs send_event_args { get; private set; }

    MessageResolver message_resolver_; // 수신 바이트를 패킷 경계로 재조립하는 도구

    public UserToken()
    {
        message_resolver_ = new MessageResolver();
    }

    // NetworkService가 이 세션에 쓸 송/수신 SAEA를 묶어준다.
    public void SetEventArgs(SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
    {
        receive_event_args = receive_args;
        send_event_args = send_args;
    }

    // 소켓이 받은 raw 바이트가 들어오는 입구.
    // TODO: message_resolver_.OnReceive(...)에 연결해 패킷 한 통이 완성되면 처리하도록 한다(아직 비어 있음).
    public void OnReceive(byte[] buffer, int offset, int transferred)
    {

    }

    // 비동기 송신 완료 후처리.
    // TODO: 송신 큐에 남은 패킷이 있으면 이어서 보내는 로직을 넣는다(아직 비어 있음).
    public void ProcessSend(SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {

        }
    }

    // 연결 종료 시 자원 정리. 소켓을 닫고 토큰 참조를 끊고 리졸버 버퍼를 비운다.
    public void Close()
    {
        Socket.Close();
        Socket = null;

        send_event_args.UserToken = null;
        receive_event_args.UserToken = null;

        message_resolver_.ClearBuffer();
    }
}
