using System.Net;
using System.Net.Sockets;

namespace Shared.Net;

// 서버 소켓을 열고(bind/listen) 클라이언트 접속을 받아들이는(accept) 역할만 한다.
// 접속이 들어오면 "직접 처리하지 않고" callback_on_newclient 델리게이트로 넘긴다
// → 소켓을 받는 부분(Listener)과 그 연결로 무엇을 할지(NetworkService)를 분리하기 위함.
public class Listener
{
    Socket listen_socket_; // 접속을 기다리는 리스닝 소켓

    // 새 접속이 들어왔을 때 외부(NetworkService)가 처리하도록 연결해 두는 콜백.
    public delegate void NewClientHandler(Socket client_socket, object? token);
    public NewClientHandler? callback_on_newclient;

    // 지정한 host:port 로 소켓을 열고 accept 루프를 시작한다.
    public void Start(string host, int port, int backlog)
    {
        this.listen_socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // "0.0.0.0" 은 모든 네트워크 인터페이스에서 접속을 받겠다는 의미.
        IPAddress address = host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(host);
        IPEndPoint endpoint = new IPEndPoint(address, port);

        listen_socket_.Bind(endpoint);   // 주소·포트 점유
        listen_socket_.Listen(backlog);  // backlog = accept 대기 큐 크기

        // accept 루프를 백그라운드에서 시작. await 자체가 "한 번에 하나씩" 흐름을 제어하므로
        // (책 원본의) 별도 스레드·AutoResetEvent·Completed 콜백이 필요 없다.
        _ = AcceptLoop();
    }

    private async Task AcceptLoop()
    {
        while (true)
        {
            try
            {
                // 다음 접속이 완료될 때까지 비동기 대기 → await 덕분에 동시에 하나만 수락된다.
                Socket client_socket = await listen_socket_.AcceptAsync();

                // 받은 소켓만 넘기고 이후 처리는 외부 델리게이트에 위임(소켓 처리부 ↔ 콘텐츠부 분리).
                callback_on_newclient?.Invoke(client_socket, null);
            }
            catch (ObjectDisposedException)
            {
                // 리스너 소켓이 닫힘 → 루프 종료
                break;
            }
            catch (SocketException)
            {
                // 개별 accept 실패는 무시하고 다음 연결을 계속 받는다.
            }
        }
    }
}
