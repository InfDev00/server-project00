using System.Net.Sockets;
using System.Security.Cryptography;
using Shared.Common;

namespace Shared.Net;

// 네트워크 계층의 총괄 클래스. 다음을 한곳에서 관리한다:
//  - Listener(접속 수락) 연결
//  - 송신/수신 SAEA 풀과 버퍼 매니저 초기화·재사용
//  - 접속 → 토큰 생성 → 수신 루프 시작까지의 흐름
// 게임 로직(콘텐츠)은 session_created_callback 으로만 연결되어, 네트워크와 분리된다.
public class NetworkService
{
    Listener client_listener_;

    // 같은 클라이언트라도 수신용·송신용 SAEA가 따로 필요하므로 풀을 둘로 나눈다.
    SocketAsyncEventArgsPool receive_event_args_pool_;
    SocketAsyncEventArgsPool send_event_args_pool_;

    BufferManager buffer_manager_; // 수신 SAEA들이 나눠 쓸 거대한 단일 버퍼

    // 새 세션이 만들어졌을 때 게임 로직 쪽으로 알려주는 콜백(콘텐츠가 등록).
    public delegate void SessionHandler(UserToken token);
    public SessionHandler session_created_callback { get; set; }

    // 서버 시작 시 1회 호출. 풀과 버퍼를 최대 동접 수만큼 미리 만들어 둔다(런타임 할당 최소화).
    public void Initialize(int max_connections, int buffer_size)
    {
        int pre_alloc_count = 1; // 연결당 미리 잡을 버퍼 개수(수신용 1개)

        buffer_manager_ = new BufferManager(max_connections * buffer_size * pre_alloc_count, buffer_size);
        receive_event_args_pool_ = new SocketAsyncEventArgsPool(max_connections);
        send_event_args_pool_ = new SocketAsyncEventArgsPool(max_connections);

        // 최대 동접 수만큼 SAEA를 미리 생성해 풀에 채운다.
        for (int i = 0; i < max_connections; ++i)
        {
            // 수신용 SAEA: 완료 콜백 연결 + 버퍼 매니저에서 구간 배정.
            // UserToken은 접속 시점에 붙이므로 여기서는 null.
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
                args.UserToken = null;

                buffer_manager_.SetBuffer(args);
                receive_event_args_pool_.Push(args);
            }
            // 송신용 SAEA: 보낼 때 버퍼를 그때그때 지정하므로 미리 버퍼를 잡지 않는다.
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
                args.UserToken = null;

                args.SetBuffer(null, 0, 0);
                send_event_args_pool_.Push(args);
            }
        }
    }

    // 리스너를 만들어 접속 콜백(OnNewClient)을 연결하고 수신 대기를 시작한다.
    public void Listen(string host, int port, int backlog)
    {
        Listener client_listener = new Listener();
        client_listener.callback_on_newclient += OnNewClient;
        client_listener.Start(host, port, backlog);
    }

    // 새 클라이언트가 접속할 때마다 호출된다(Listener가 위임).
    private void OnNewClient(Socket socket, object token)
    {
        // 이 연결이 쓸 수신·송신 SAEA를 풀에서 하나씩 꺼낸다.
        SocketAsyncEventArgs receive_args = receive_event_args_pool_.Pop();
        SocketAsyncEventArgs send_args = send_event_args_pool_.Pop();

        // 이 연결 전용 세션(UserToken)을 새로 만들어 두 SAEA가 같은 토큰을 가리키게 한다.
        // → 송신 완료/수신 완료 어느 콜백에서 꺼내도 동일한 세션을 얻는다.
        UserToken user_token = new UserToken();
        receive_args.UserToken = user_token;
        send_args.UserToken = user_token;

        // 게임 로직 쪽에 "새 세션 생겼다"고 알린다(콘텐츠가 IPeer 바인딩 등을 함).
        session_created_callback?.Invoke(user_token);

        // 수신 루프 시작.
        BeginReceive(socket, receive_args, send_args);
    }

    // 토큰에 소켓·SAEA를 묶고 첫 비동기 수신을 건다.
    private void BeginReceive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
    {
        UserToken token = receive_args.UserToken as UserToken;
        token.SetEventArgs(receive_args, send_args);

        token.Socket = socket;

        // ReceiveAsync가 동기적으로 즉시 완료되면 Completed 콜백이 안 불리므로 직접 처리한다.
        if (!socket.ReceiveAsync(receive_args))
            ProcessReceive(receive_args);
    }

    // 실제 수신 처리. 받은 바이트를 토큰에 넘기고, 곧바로 다음 수신을 다시 건다(수신 루프).
    private void ProcessReceive(SocketAsyncEventArgs args)
    {
        UserToken token = args.UserToken as UserToken;
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            // 받은 데이터를 세션에 전달(여기서 메시지 프레이밍·패킷 처리로 이어진다).
            token.OnReceive(args.Buffer, args.Offset, args.BytesTransferred);

            // 다음 데이터를 계속 받기 위해 또 ReceiveAsync. (동기 완료 시 직접 처리)
            if (!token.Socket.ReceiveAsync(args))
                ProcessReceive(args);
        }
        else
        {
            // BytesTransferred == 0 은 상대가 정상 종료한 것. 그 외는 에러.
            Console.WriteLine("error {0}, transferred {1}", args.SocketError, args.BytesTransferred);
            token.Close();
        }
    }

    // 비동기 수신이 완료되면 호출되는 통합 콜백 → 실제 처리는 ProcessReceive로 위임.
    private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.LastOperation == SocketAsyncOperation.Receive)
        {
            ProcessReceive(args);
        }
    }

    // 비동기 송신이 완료되면 호출되는 콜백 → 세션의 송신 후처리로 위임.
    private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
    {
        UserToken token = args.UserToken as UserToken;
        token.ProcessSend(args);
    }
}
