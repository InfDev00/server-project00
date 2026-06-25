using UnityEngine;

// ============================================================
// SystemManager - 씬 전환에도 살아남는 영속 시스템 루트
// DontDestroyOnLoad로 유지되어 게임 전반의 공유 상태를 보관한다.
// ============================================================
public class SystemManager : MonoBehaviour
{
    static SystemManager _instance;
    public static SystemManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindFirstObjectByType<SystemManager>();
            }
            return _instance;
        }
    }

    // 현재 턴 플레이어 ID (게임 시작/턴 통지로 갱신). 내 ID와 비교해 내 턴 판단.
    public int TurnPlayerID;

    void Start()
    {
        DontDestroyOnLoad(this);   // 씬 전환에도 파괴되지 않게 영속화
    }
}
