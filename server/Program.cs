

public class Program
{
    static void Main(string[] args)
    {
        NetworkService service = new NetworkService();
        service.OnSessionCreated += OnSessionCreated;
        service.Initialize();

        service.Listen("0.0.0.0", 7979, 100);

        Console.WriteLine("서버 시작");
        while (true)
        {
            Thread.Sleep(1000);
            Console.ReadKey();
        }
    }

    static void OnSessionCreated(Session session)
    {
        Console.WriteLine("[Server] 클라이언트 접속 — 세션 생성");
        User user = new User(session);
    }
}
