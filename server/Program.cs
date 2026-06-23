

public class Program
{
    static void Main(string[] args)
    {
        NetworkService service = new NetworkService();
        service.Initialize();

        service.Listen("0.0.0.0", 7979, 100);

        Console.WriteLine("서버 시작");
        while (true)
        {
            Thread.Sleep(1000);
            Console.ReadKey();
        }
    }
}
