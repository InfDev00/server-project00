// ============================================================
// GameRoomManager - 방 레지스트리 + 매처
// 여러 방을 보유(roomId 발급)하고, 입장 요청을 받아 "현재 입장 받는
// 방"을 채운다. 그 방이 차면 Playing으로 넘기고 다음 입장은 새 방을
// 만든다 → 여러 방이 동시에 공존. 방 선택+추가+상태전환은 매니저 락
// 단일 임계영역에서 원자적으로 처리(정원 초과·중복 매칭 방지).
// 소켓 Send는 항상 락 밖에서 한다. 락 순서는 매니저 → 방 으로 고정.
// ============================================================
public class GameRoomManager
{
    readonly object _lock = new();
    readonly Dictionary<int, GameRoom> _rooms = new();

    GameRoom? _openRoom;        // 현재 입장 받는 방(없으면 새로 생성)
    int _nextRoomId = 1;

    readonly int _capacity;     // 방 정원

    public GameRoomManager(int capacity)
    {
        _capacity = capacity;
    }

    // 대기방 입장 — 빈 방에 넣고, 정원이 차면 게임 시작을 알린다.
    public void Enqueue(User user)
    {
        GameRoom room;
        bool started;

        lock (_lock)
        {
            // 입장 받는 방이 없거나 더는 못 받으면 새 방 생성
            if (_openRoom == null || _openRoom.State != RoomState.Waiting)
            {
                _openRoom = new GameRoom(_nextRoomId++, _capacity);
                _rooms[_openRoom.Id] = _openRoom;
            }

            room = _openRoom;
            room.TryAdd(user);          // 매니저 락 안에서 호출 (락 순서 매니저→방)

            started = room.State == RoomState.Playing;

            if (started)
                _openRoom = null;       // 다음 입장은 새 방으로
        }

        // --- 락 밖: 입장 통지는 방이 소유 ---
        room.OnUserEnter(user);

        if (started)
            Console.WriteLine($"[Room {room.Id}] 정원 충족 → 로딩 시작");
    }

    // 로딩 완료 보고 — 방 전원이 완료되면 게임 시작을 broadcast (준비 배리어)
    public void OnLoadingComplete(User user)
    {
        GameRoom? room = user.CurrentRoom;
        if (room == null)
        {
            Console.WriteLine($"[로딩완료] {user.Username} — 속한 방 없음(무시)");
            return;
        }

        bool allReady = room.MarkLoaded(user);
        Console.WriteLine($"[로딩완료] Room {room.Id}: {user.Username} 보고 → {room.LoadedCount}/{room.Count} (전원완료={allReady})");

        if (!allReady) return;

        room.Logic.StartGame();     // 게임 상태 초기화(점수·첫 턴)

        var start = Packet.Create<GameStartNotifyPacket>();
        start.RoomId = (short)room.Id;
        start.TurnPlayerID = room.FirstTurnPlayerID;
        room.Broadcast(start.Pack());

        Console.WriteLine($"[Room {room.Id}] 전원 로딩 완료 → 게임 시작 broadcast ({room.Count}명)");
    }

    // 접속 해제 등으로 유저가 방을 떠날 때 정리
    public void OnUserLeft(User user)
    {
        GameRoom? room;
        bool notify = false;

        lock (_lock)
        {
            room = user.CurrentRoom;
            if (room == null) return;

            room.Remove(user);
            user.CurrentRoom = null;

            if (room.State == RoomState.Closed)
            {
                // 빈 방 회수
                _rooms.Remove(room.Id);
                if (_openRoom == room)
                    _openRoom = null;
            }
            else
            {
                notify = true;
            }
        }

        // 방이 살아있다면 남은 인원에 갱신 통지 (락 밖)
        if (notify)
        {
            var state = Packet.Create<RoomStateNotifyPacket>();
            state.Current = (short)room!.Count;
            state.Needed = (short)room.Capacity;
            room.Broadcast(state.Pack());
        }
    }
}
