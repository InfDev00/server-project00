public interface IPeer
{
    void OnMessage(Packet packet);

    void OnRemoved();

    void Send(Packet packet);

    void Disconnected();
}