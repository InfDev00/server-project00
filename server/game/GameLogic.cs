// ============================================================
// GameLogic - 방 1개의 게임 규칙(블랙잭형 굴리기). 서버 권위.
// 플레이어별 누적 점수와 현재 턴/게임오버를 들고, Roll/Stop 요청을
// 검증·처리해 턴 결과를 방에 broadcast한다. 여러 워커 스레드 접근 →
// 전용 락으로 직렬화. Pack까지만 락 안, 실제 Send(Broadcast)는 락 밖.
// ============================================================
public class GameLogic
{
    class Player
    {
        public int Number;      // 누적 합
        public bool Stopped;    // 스톱 선언 여부
    }

    readonly GameRoom _room;
    readonly Random _rand = new();
    readonly object _lock = new();

    readonly Dictionary<int, Player> _players = new();
    int _turn;      // 현재 턴 플레이어 ID
    bool _over;     // 게임 종료 여부

    public GameLogic(GameRoom room)
    {
        _room = room;
    }

    // 게임 시작 — 점수 초기화, 첫 턴은 먼저 들어온 유저(최소 ID)
    public void StartGame()
    {
        lock (_lock)
        {
            _players.Clear();
            _over = false;
            _turn = _room.FirstTurnPlayerID;
        }
    }

    // 굴리기 — 1~10 누적 후 턴 종료 처리
    public void Roll(int uid)
    {
        byte[] data;
        lock (_lock)
        {
            if (_over || uid != _turn) return;          // 턴·상태 검증(서버 권위)

            int result = _rand.Next(1, 11);             // 1~10
            GetPlayer(uid).Number += result;
            data = EndTurn(uid, result);
        }
        _room.Broadcast(data);
    }

    // 스톱 — 더 굴리지 않음. 턴 종료 처리(결과 없음)
    public void Stop(int uid)
    {
        byte[] data;
        lock (_lock)
        {
            if (_over || uid != _turn) return;

            GetPlayer(uid).Stopped = true;
            data = EndTurn(uid, 0);
        }
        _room.Broadcast(data);
    }

    // 턴 종료 — 종료 판정 후 통지 패킷 바이트 반환 (호출 시 _lock 보유 가정)
    // 종료 조건: 버스트(>21) 또는 전원 스톱. 아니면 턴을 상대에게 넘긴다.
    private byte[] EndTurn(int uid, int result)
    {
        Player me = _players[uid];

        bool bust = me.Number > 21;
        bool allStopped = _players.Count == _room.Capacity && AllStopped();
        _over = bust || allStopped;

        int winner = -1;
        if (_over)
            winner = bust ? _room.GetOtherUserID(uid)   // 버스트 → 상대 승
                          : HighestScorer();            // 전원 스톱 → 최고점 승(동점 -1)
        else
        {
            var otherId = _room.GetOtherUserID(uid);          // 게임 계속 → 턴 전환
            if (!_players.TryGetValue(otherId, out var other) || !other.Stopped)
                _turn = otherId;

        }

        var p = Packet.Create<TurnResultNotifyPacket>();
        p.UID = uid;
        p.Result = result;
        p.Total = me.Number;
        p.IsGameOver = (short)(_over ? 1 : 0);
        p.TurnPlayerID = _turn;
        p.WinnerID = winner;
        return p.Pack();
    }

    // 최고 점수 플레이어 ID (동점이면 -1=무승부). 전원 스톱 시엔 모두 21 이하.
    private int HighestScorer()
    {
        int bestId = -1, bestNum = int.MinValue;
        bool tie = false;
        foreach (var kv in _players)
        {
            if (kv.Value.Number > bestNum)
            {
                bestNum = kv.Value.Number;
                bestId = kv.Key;
                tie = false;
            }
            else if (kv.Value.Number == bestNum)
            {
                tie = true;
            }
        }
        return tie ? -1 : bestId;
    }

    // uid의 Player를 가져오되 없으면 생성
    private Player GetPlayer(int uid)
    {
        if (!_players.TryGetValue(uid, out var player))
            _players[uid] = player = new Player();
        return player;
    }

    private bool AllStopped()
    {
        foreach (var player in _players.Values)
            if (!player.Stopped) return false;
        return true;
    }
}
