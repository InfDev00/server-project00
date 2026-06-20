// 서버 실행 진입점.
// NetworkService를 조립해 접속을 받는 서버를 띄운다.
//   Initialize(풀·버퍼 준비) → session_created_callback(세션 생기면 알림) 등록 → Listen(접속 대기)
//
// 접속 확인용: telnet 127.0.0.1 7979 로 연결하면 아래 콜백이 불려 로그가 찍힌다.

using Shared.Net;

public class Program
{
    const int MaxConnections = 1000; // 최대 동시 접속 수(풀·버퍼를 이만큼 미리 잡는다)
    const int BufferSize = 1024;     // 연결당 수신 버퍼 크기(byte)
    const int Port = 7979;
    const int Backlog = 100;         // accept 대기 큐 크기

    static void Main(string[] args)
    {
        var service = new NetworkService();
        service.Initialize(MaxConnections, BufferSize);

        // 새 클라이언트가 접속해 세션(UserToken)이 만들어질 때마다 호출된다.
        // 주의: 이 콜백은 BeginReceive보다 먼저 불려 아직 token.Socket이 설정되기 전이다.
        //       (콘텐츠 계층이 IPeer 바인딩 등을 하는 자리이지 소켓을 읽는 자리가 아니다.)
        //       지금은 접속이 들어오는지 눈으로 확인하는 용도로 로그만 찍는다.
        service.session_created_callback += (UserToken token) =>
        {
            Console.WriteLine("[접속] 새 세션 생성됨");
        };

        service.Listen("0.0.0.0", Port, Backlog);

        Console.WriteLine($"서버 시작. 포트 {Port} 에서 접속 대기 중...");
        Console.WriteLine("종료하려면 Enter 키를 누르세요.");
        Console.ReadLine();
    }
}
