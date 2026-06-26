// 방 수명 상태: 채우는 중 → 게임 중 → (비면) 폐기
public enum RoomState { Waiting, Playing, Closed }

// ============================================================
// GameRoom - 한 게임 방. 자신의 인원/정원/상태를 원자적으로 관리
// 유저를 ID로 인덱싱(Dictionary)해 조회·타깃 전송을 O(1)로 처리한다.
// 여러 IOCP 워커 스레드가 동시에 접근하므로 방 전용 락으로 보호하고,
// 소켓 Send는 항상 락 밖에서 한다(스냅샷 후 전송).
// ============================================================
public class GameRoom
{
    public int Id { get; }
    public int Capacity { get; }

    readonly object _lock = new();

    readonly Dictionary<int, User> _users = new();
    readonly HashSet<int> _loaded = new();   // 로딩 완료 보고한 유저 ID(준비 배리어)

    public RoomState State { get; private set; } = RoomState.Waiting;

    public GameLogic Logic;

    public GameRoom(int id, int capacity)
    {
        Id = id;
        Capacity = capacity;

        Logic = new GameLogic(this);
    }

    // 입장 시도 — Waiting이고 여유가 있을 때만 추가. 정원이 차면 Playing 전환.
    public bool TryAdd(User user)
    {
        lock (_lock)
        {
            if (State != RoomState.Waiting || _users.Count >= Capacity)
                return false;

            _users[user.ID] = user;
            user.CurrentRoom = this;

            if (_users.Count >= Capacity)
                State = RoomState.Playing;

            return true;
        }
    }

    // 퇴장 — 비면 Closed 전환(매니저가 폐기 판단에 사용)
    public bool Remove(User user)
    {
        lock (_lock)
        {
            bool removed = _users.Remove(user.ID);
            _loaded.Remove(user.ID);
            if (removed && _users.Count == 0)
                State = RoomState.Closed;
            return removed;
        }
    }

    // 로딩 완료 보고 — 방 전원이 완료되면 true(게임 시작 조건).
    // 멤버가 아닌 유저나 중복 보고는 무시(HashSet이 dedup). 떠난 멤버로
    // 인원이 줄어든 경우도 현재 인원 기준으로 판정한다.
    public bool MarkLoaded(User user)
    {
        lock (_lock)
        {
            if (!_users.ContainsKey(user.ID))
                return false;

            _loaded.Add(user.ID);
            return _users.Count > 0 && _loaded.Count >= _users.Count;
        }
    }

    public int Count
    {
        get { lock (_lock) return _users.Count; }
    }

    public int LoadedCount
    {
        get { lock (_lock) return _loaded.Count; }
    }

    // 첫 턴 플레이어 ID — 가장 작은 ID(먼저 들어온 유저)가 선턴.
    // 락 안에서 읽어 입장/퇴장과의 동시 수정 충돌을 막는다. 빈 방이면 -1.
    public int FirstTurnPlayerID
    {
        get
        {
            lock (_lock)
            {
                int min = -1;
                foreach (int id in _users.Keys)
                    if (min == -1 || id < min) min = id;
                return min;
            }
        }
    }

    // 유저 입장 직후 통지 — 본인에게 입장 결과, 방 전원에 인원 상태,
    // 정원이 충족됐으면(Playing) 로딩 시작까지. 매니저 락 밖에서 호출.
    public void OnUserEnter(User user)
    {
        // 입장한 본인에게 결과 (unicast)
        var ack = Packet.Create<RoomEnterAckPacket>();
        ack.RoomId = (short)Id;
        ack.UID = user.ID;
        ack.Current = (short)Count;
        ack.Needed = (short)Capacity;
        user.Send(ack);

        // 방 전원에 현재 인원 통지
        var state = Packet.Create<RoomStateNotifyPacket>();
        state.Current = (short)Count;
        state.Needed = (short)Capacity;
        Broadcast(state.Pack());

        // 정원 충족 → 로딩 시작 통지
        if (State == RoomState.Playing)
        {
            var loading = Packet.Create<LoadingNotifyPacket>();
            loading.RoomId = (short)Id;
            Broadcast(loading.Pack());
        }
    }

    // 주어진 ID가 아닌 다른 멤버의 ID (2인 방에서 상대 찾기). 없으면 -1.
    public int GetOtherUserID(int id)
    {
        lock (_lock)
        {
            foreach (int key in _users.Keys)
                if (key != id) return key;
            return -1;
        }
    }

    // 같은 방 전원에게 송신. 스냅샷만 락 안에서 뜨고 실제 Send는 락 밖에서.
    public void Broadcast(byte[] data)
    {
        User[] snapshot = Snapshot();
        for (int i = 0; i < snapshot.Length; ++i)
            snapshot[i].Send(data);
    }

    // 현재 유저 배열 스냅샷 (락 안에서 복사, 전송은 호출부가 락 밖에서)
    User[] Snapshot()
    {
        lock (_lock)
        {
            var arr = new User[_users.Count];
            _users.Values.CopyTo(arr, 0);
            return arr;
        }
    }
}
