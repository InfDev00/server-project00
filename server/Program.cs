// ============================================================
// Program - 게임 서버 진입점
// NetworkService를 초기화해 7979 포트로 접속을 받고,
// 접속마다 User(IPeer)를 만들어 세션에 바인딩한다.
// ============================================================
public class Program
{
    // 전역 매칭 매니저 (모든 User가 공유). 정원 2명으로 시작.
    public static GameRoomManager RoomManager { get; private set; } = new GameRoomManager(2);

    // User ID 발급 카운터 (Interlocked로 스레드 안전하게 증가)
    static int _nextUserID = 0;

    static void Main(string[] args)
    {
        NetworkService service = new NetworkService();
        service.OnSessionCreated += OnSessionCreated;   // 접속 시 콜백 등록
        service.Initialize();

        service.Listen("0.0.0.0", 7979, 100);           // 모든 인터페이스 7979 리슨

        Console.WriteLine("서버 시작");
        while (true)                                     // 메인 스레드 유지
        {
            Thread.Sleep(1000);
            Console.ReadKey();
        }
    }

    // 새 세션 생성 시 호출 — 게임 로직(User)을 세션에 연결
    static void OnSessionCreated(Session session)
    {
        Console.WriteLine("[Server] 클라이언트 접속 — 세션 생성");

        var id = Interlocked.Increment(ref _nextUserID);   // 고유 ID 발급
        User user = new User(session, id);                 // 세션에 게임 로직 바인딩
    }
}
